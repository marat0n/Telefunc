module Telefunc.Infrastructure
open Telegram.Bot
open Telegram.Bot.Types
open System.Threading.Tasks
open System

type UpdateHandler = delegate of ITelegramBotClient * Update -> bool

let inline internal awaitTask (task: Task) =
    task |> Async.AwaitTask |> Async.RunSynchronously

let inline internal awaitTypedTask task =
    task |> Async.AwaitTask |> Async.RunSynchronously

let inline internal onEnterLine fn =
    Console.ReadLine() |> ignore
    fn ()