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

type internal MaybeBuilder() =
    member _.Bind (x, f) =
        match x with
        | None -> None
        | Some a -> f a

    member _.Zero () = ()

    member _.Return (x) = Some x

    member _.ReturnFrom (x) = x

let internal maybe = new MaybeBuilder()

let inline internal isNull (x: 'T when 'T: not struct) = obj.ReferenceEquals(x, null)

let inline internal nullableToOption x = if isNull x then None else Some x

type internal MaybeNullable() =
    member _.Bind(x, fn) =
        match nullableToOption x with
        | None -> None
        | Some value -> fn value

    member _.Return x = Some x

let internal maybeNullable = MaybeNullable()