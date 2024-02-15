# Telefunc
The functional wrapping library of [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) wrote in F#.

## Roadmap
- 0.0.1
  - Starting point :)
- 0.1 (we are here)
  1. ✅ runBot function;
  2. ✅ tree-like bot's handlers structure;
  3. ✅ basic wrapped functions for sending messages;
  4. ✅ states machine;
  5. ✅ helper function `getId: Update -> int64 option`.
- 0.2
  1. ✅ more updates-filtering functions for handlers;
  2. more Telegram.Bot wrapped functions and types.
- 0.3
  1. more comfortable state-machine mechanism;

  ... _there will be more_ ...

## Examples
### [HelloWorld.fsx](https://github.com/marat0n/Telefunc/blob/main/Examples/HelloWorld.fsx)

```fsharp
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
    "TOKEN"
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
```
