#r "nuget: Telefunc, 0.1.0"

open Telefunc.Core
open Telegram.Bot.Types
open Telegram.Bot
open Telegram.Bot.Exceptions
open System.Threading.Tasks

exception ApiRequestException_fs of ApiRequestException

let handlePollingErrorAsync _ ex _ =
    task {
        printfn "%A"
        <| match ex with
            | ApiRequestException_fs apiReqException ->
                $"Telegram API Error:\n[{apiReqException.ErrorCode}]\n{apiReqException.Message}"
            | _ -> ex.ToString()
    } :> Task

let inline sayHello name (bot: ITelegramBotClient) (update: Update) =
    Wrappers.sendMessage(
        bot = bot,
        chatId = update.Message.Chat.Id,
        text = $"Hello {name}!",
        insteadOfMsg = None )

let sayHelloWorld bot update =
    sayHello "World" bot update |> ignore
    true

let sayHelloToUser (bot: ITelegramBotClient) (update: Update) =
    sayHello update.Message.Chat.FirstName bot update |> ignore
    true

let help (bot: ITelegramBotClient) (update: Update) =
    Wrappers.sendMessage(
        bot = bot,
        chatId = update.Message.Chat.Id,
        text = "Welcome to Hello World bot!\n\nCommands:\n/start\n/help\n\nWrite \"Hello\" to me and I will respond!",
        insteadOfMsg = None ) |> ignore
    true

runBot
    "1062959848:AAFEYlO7Vic24sWFtt1vWem8BPX_s7Li284"
    [
        Filter.command [
            Filter.commandName "start" [ sayHelloWorld ]
            Filter.commandName "help" [ help ]
        ]
        Filter.message [
            Filter.justText "Hello" [ sayHelloToUser ]
        ]
    ]
    handlePollingErrorAsync
