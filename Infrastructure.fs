module Telefunc.Infrastructure

open Telegram.Bot
open Telegram.Bot.Types
open System.Threading.Tasks
open System

type UpdateHandler = delegate of ITelegramBotClient * Update -> bool

let inline awaitTask (task: Task) =
    task |> Async.AwaitTask |> Async.RunSynchronously

let inline awaitTypedTask task =
    task |> Async.AwaitTask |> Async.RunSynchronously

let inline onEnterLine fn =
    Console.ReadLine() |> ignore
    fn ()

type MaybeBuilder() =
    member _.Bind (x, f) =
        match x with
        | None -> None
        | Some a -> f a

    member _.Zero () = ()

    member _.Return (x) = Some x

    member _.ReturnFrom (x) = x

let maybe = new MaybeBuilder()

let inline isNull (x: 'T when 'T: not struct) = obj.ReferenceEquals(x, null)

let inline nullableToOption x = if isNull x then None else Some x

type MaybeNullable() =
    member _.Bind(x, fn) =
        match nullableToOption x with
        | None -> None
        | Some value -> fn value

    member _.Return x = Some x

let maybeNullable = MaybeNullable()