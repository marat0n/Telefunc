﻿module Telefunc

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