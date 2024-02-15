module Telefunc.Core

open Telegram.Bot
open Telegram.Bot.Types
open Telegram.Bot.Polling
open Telegram.Bot.Exceptions
open Telegram.Bot.Types.Enums
open Telegram.Bot.Types.ReplyMarkups
open System.Threading
open System.Threading.Tasks
open Telefunc.Infrastructure


(*-----------------------
    Primary functions
-----------------------*)

let inline getId (update: Update) =
    match update.Type with
    | UpdateType.Message -> Some update.Message.Chat.Id
    | UpdateType.InlineQuery -> Some update.InlineQuery.From.Id
    | UpdateType.ChosenInlineResult -> Some update.ChosenInlineResult.From.Id
    | UpdateType.CallbackQuery -> Some update.CallbackQuery.From.Id
    | UpdateType.EditedMessage -> Some update.EditedMessage.Chat.Id
    | UpdateType.ChannelPost -> Some update.ChannelPost.Chat.Id
    | UpdateType.EditedChannelPost -> Some update.EditedChannelPost.Chat.Id
    | UpdateType.ShippingQuery -> Some update.ShippingQuery.From.Id
    | UpdateType.PreCheckoutQuery -> Some update.PreCheckoutQuery.From.Id
    | UpdateType.PollAnswer -> Some update.PollAnswer.User.Id
    | UpdateType.MyChatMember -> Some update.MyChatMember.Chat.Id
    | UpdateType.ChatMember -> Some update.ChatMember.Chat.Id
    | UpdateType.ChatJoinRequest -> Some update.ChatJoinRequest.Chat.Id
    | _ -> None

let inline newBot (token: string) = new TelegramBotClient(token)

let inline private updatesChecker
    (handlers: UpdateHandler list)
    (update: Update)
    (bot: ITelegramBotClient)
    =
    handlers
    |> Seq.tryFind (fun handler -> handler.Invoke(bot, update))
    |> Option.isSome

let inline private handleUpdates
    (updatesChecker)
    (bot: ITelegramBotClient)
    (update: Update)
    (_)
    =
    task {
        let gotUpdate = updatesChecker update bot

        if gotUpdate then ()
        else
            raise (new ApiRequestException("Didn't match any branch."))
    }
    :> Task

let inline runBot
    (token: string)
    (updates: UpdateHandler list)
    (excHandler: ITelegramBotClient -> exn -> CancellationToken -> Task)
    =
    use cts = new CancellationTokenSource()

    let receiverOptions = new ReceiverOptions()
    receiverOptions.AllowedUpdates <- Array.empty<UpdateType>

    let bot = newBot token

    bot.StartReceiving(
        updateHandler       = (handleUpdates <| updatesChecker updates),
        pollingErrorHandler = excHandler,
        receiverOptions     = receiverOptions,
        cancellationToken   = cts.Token
    )

    let me = bot.GetMeAsync() |> awaitTypedTask

    printfn "Start listening for %s (%d)" me.Username me.Id

    onEnterLine cts.Cancel


(*-----------------------
    Updates filtering
-----------------------*)

module Filter =
    open System.Text.RegularExpressions
    open Telefunc.Sscanf
    open Telefunc.State

    let private commandRegex = Regex(@"^\/[\S]+", RegexOptions.Compiled)

    let inline getMessageText (update: Update) =
        maybeNullable {
            let! message = update.Message
            let! text = message.Text
            return text
        }

    let inline private scan format text =
        try Some (sscanf format text)
        with _ -> None

    let inline checkUpdateIsCommand (upd: Update) =
        match getMessageText upd with
        | Some value -> commandRegex.IsMatch value
        | None -> false

    let inline isCommand (handlers: UpdateHandler list) (bot: ITelegramBotClient) (update: Update) =
        checkUpdateIsCommand update
        && updatesChecker handlers update bot

    let inline command
        (name: string)
        (handlers: UpdateHandler list)
        (bot: ITelegramBotClient)
        (update: Update)
        =
        match getMessageText update with
        | Some value ->
            if $"/{name}" = value
            then updatesChecker handlers update bot
            else false
        | None -> false

    let inline commandScan
        (format: PrintfFormat<_, _, _, _, 't>)
        (handler: 't -> UpdateHandler list)
        (bot: ITelegramBotClient) (update: Update)
        =
        checkUpdateIsCommand update
        &&
        update.Message.Text.Substring(1)
        |> scan format
        |> Option.map (fun x ->
            updatesChecker (handler x) update bot)
        |> Option.defaultValue false

    let inline isMessage (handlers: UpdateHandler list) (bot: ITelegramBotClient) (update: Update) =
        match isNull update.Message with
        | true -> false
        | false -> updatesChecker handlers update bot

    let inline hasMsgText (handlers: UpdateHandler list) (bot: ITelegramBotClient) (update: Update) =
        match isNull update.Message.Text with
        | true -> false
        | false -> updatesChecker handlers update bot

    let inline hasIncludedText (inclText: string) (handlers: UpdateHandler list) (bot: ITelegramBotClient) (update: Update) =
        maybeNullable {
            let! text = update.Message.Text
            return text.Contains(inclText)
        }
        |> Option.map (fun contains ->
            contains
            && updatesChecker handlers update bot)
        |> Option.defaultValue false

    let inline msgText (text: string) (handlers: UpdateHandler list) (bot: ITelegramBotClient) (update: Update) =
        match isNull update.Message.Text with
        | true -> false
        | false ->
            update.Message.Text = text
            && updatesChecker handlers update bot
    
    let inline messageScan
        (format: PrintfFormat<_, _, _, _, 't>)
        (handler: 't -> UpdateHandler list)
        (bot: ITelegramBotClient) (update: Update)
        =
        maybe {
            let! text = getMessageText update
            return!
                scan format text
                |> Option.map (fun x ->
                    updatesChecker (handler x) update bot)
        } |> Option.defaultValue false

    let inline isCallback (handlers: UpdateHandler list) (bot: ITelegramBotClient) (update: Update) =
        match isNull update.CallbackQuery with
        | true -> false
        | false -> updatesChecker handlers update bot

    let inline callbackData data (handlers: UpdateHandler list) (bot: ITelegramBotClient) (update: Update) =
        match isNull update.CallbackQuery.Data with
        | true -> false
        | false ->
            update.CallbackQuery.Data = data
            && updatesChecker handlers update bot

    let inline callbackScan
        (format: PrintfFormat<_, _, _, _, 't>)
        (handler: 't -> UpdateHandler list)
        (bot: ITelegramBotClient) (update: Update)
        =
        match isNull update.CallbackQuery.Data with
        | true -> false
        | false ->
            let scan text =
                try Some (sscanf format text)
                with _ -> None
            update.CallbackQuery.Data
            |> scan
            |> Option.map (fun x ->
                updatesChecker (handler x) update bot)
            |> Option.defaultValue false

    let inline byState<'s when 's :> TelefuncState>
        (handlers: (('s -> bool) * UpdateHandler list) list)
        (bot: ITelegramBotClient)
        (update: Update)
        =
        handlers
        |> Seq.tryFind (fun (stateChecker, updHandler) ->
            // let (stateChecker, updHandler) = hndlr
            let st = maybe {
                let! id = getId update
                let! state = bot.State.get id
                return stateChecker (state :?> 's)
            }
            st
            |> Option.defaultValue false
            && updatesChecker updHandler update bot)
        |> Option.isSome


(*-----------------------
    Telefunc Wrappers
-----------------------*)

[<AbstractClass>]
type Wrappers =
    static member sendMessage
        (   bot: ITelegramBotClient,
            chatId: int64,
            text: string,
            ?insteadOfMsg: int,
            ?replyMarkup: IReplyMarkup
        ) : Message =
        match insteadOfMsg with
        | Some msgId ->
            bot.DeleteMessageAsync(
                chatId = chatId,
                messageId = msgId
            ) |> awaitTask |> ignore
            bot.SendTextMessageAsync(
                chatId = chatId,
                text = text,
                parseMode = ParseMode.Html,
                replyMarkup = defaultArg replyMarkup null
            ) |> awaitTypedTask
        | None -> 
            bot.SendTextMessageAsync(
                chatId = chatId,
                text = text,
                parseMode = ParseMode.Html,
                replyMarkup = defaultArg replyMarkup null
            ) |> awaitTypedTask