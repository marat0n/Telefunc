module Telefunc

open Telegram.Bot
open Telegram.Bot.Types
open System.Threading
open Telegram.Bot.Polling
open System.Threading.Tasks
open Telegram.Bot.Types.Enums
open Telegram.Bot.Exceptions
open System

type UpdateHandler = delegate of ITelegramBotClient * Update -> bool

let inline private awaitTask (task: Task) =
    task |> Async.AwaitTask |> Async.RunSynchronously

let inline private awaitTypedTask task =
    task |> Async.AwaitTask |> Async.RunSynchronously

let inline private onEnterLine fn =
    Console.ReadLine() |> ignore
    fn ()

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

module Filter =
    open System.Text.RegularExpressions
    open Telefunc.Sscanf

    let inline private isNull (x: 'T when 'T: not struct) = obj.ReferenceEquals(x, null)

    let inline private nullableToOption x = if isNull x then None else Some x

    type private MaybeNullable() =
        member _.Bind(x, fn) =
            match nullableToOption x with
            | None -> None
            | Some value -> fn value

        member _.Return x = Some x

    let private maybeNullable = MaybeNullable()

    let private messageText (update: Update) =
        maybeNullable {
            let! message = update.Message
            let! text = message.Text
            return text
        }

    let command (handlers: UpdateHandler list) (bot: ITelegramBotClient) (update: Update) =
        match messageText update with
        | Some value ->
            let rx = Regex(@"^\/[\S]+", RegexOptions.Compiled)

            if rx.IsMatch value then
                updatesChecker handlers update bot
            else
                false
        | None -> false

    let commandName
        (name: string)
        (handlers: UpdateHandler list)
        (bot: ITelegramBotClient)
        (update: Update)
        =
        match messageText update with
        | Some value ->
            if $"/{name}" = value then
                updatesChecker handlers update bot
            else
                false
        | None -> false


