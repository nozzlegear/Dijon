namespace Dijon.Services

open Dijon
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Discord
open Discord.WebSocket
open FSharp.Control.Tasks.V2.ContextInsensitive

type MessageMonitorService(logger : ILogger<MessageMonitorService>, bot : Dijon.BotClient, database : IDijonDatabase, messages : IMessageHandler) =
    let handleMessage (message : IMessage) = 
        messages.HandleMessage message

    interface IDisposable with
        member _.Dispose() =
            ()

    interface IHostedService with
        member _.StartAsync _ = 
            task {
                // Wire event listeners
                do! bot.AddEventListener (DiscordEvent.MessageReceived handleMessage)
            } 
            :> Task
            
        member _.StopAsync _ =
            Task.CompletedTask
