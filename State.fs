module Telefunc.State
open Infrastructure
open System.Collections.Generic
open Telegram.Bot

[<AbstractClass>]
type TelefuncState() =
    abstract Compare: TelefuncState -> bool

let inline ( >-> ) (left: TelefuncState) (right: UpdateHandler list) =
    ((fun (comparable: TelefuncState) -> comparable.Compare left), right)

type TelefuncStateContainer() =
    member val private states = Dictionary<int64, TelefuncState>()

    member this.set (chatId: int64) (state: TelefuncState) =
        if this.states.ContainsKey chatId
        then this.states[ chatId ] <- state
        else this.states.Add(chatId, state)

    member this.get chatId : TelefuncState option =
        if this.states.ContainsKey chatId
        then Some this.states[chatId]
        else None

module StatesMachine =
    let BotStatesBindings = Dictionary<ITelegramBotClient, TelefuncStateContainer>()

    let Add bot =
        let newContainer = TelefuncStateContainer()
        BotStatesBindings.Add (bot, newContainer)
        newContainer

    let GetBy bot =
        if BotStatesBindings.ContainsKey bot
        then BotStatesBindings[bot]
        else Add bot


type ITelegramBotClient with
    member this.State = StatesMachine.GetBy this