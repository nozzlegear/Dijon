namespace Dijon

open System
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

type DiscordEvent = 
    | MessageReceived of (IMessage -> Async<unit>)
    | UserLeft of (SocketGuildUser -> Async<unit>)
    | UserJoined of (SocketGuildUser -> Async<unit>)
    | UserUpdated of (SocketGuildUser -> SocketGuildUser -> Async<unit>)
    | BotLeftGuild of (SocketGuild -> Async<unit>)

type BotClient(logger : ILogger<BotClient>, config : IConfiguration, database: IDijonDatabase) =
    let client = new DiscordSocketClient()
    let messageHandler : IMessageHandler = upcast MessageHandler(database, client)
    let readyEvent = new System.Threading.ManualResetEvent false
    let token =
        match config.GetValue<string>("DIJON_BOT_TOKEN") with
        | ""
        | null -> failwith "DIJON_BOT_TOKEN was null or empty."
        | token -> token 

    let singleArgFunc (fn : 'a -> Async<unit>) = 
        // Transform the F# function to a C# Func<'a, Task>
        Func<'a, Task>(fun arg -> 
            // Don't call the handler until we know the socket client is ready
            readyEvent.WaitOne() 
            |> ignore

            fn arg 
            |> Async.StartAsTask 
            :> Task
        )

    let doubleArgFunc (fn : 'a -> 'b -> Async<unit>) =
        // Transform the F# function to a C# Func<'a, 'b', Task>
        Func<'a, 'b, Task>(fun a b -> 
            // Don't call the handler until we know the socket client is ready
            readyEvent.WaitOne() 
            |> ignore

            fn a b 
            |> Async.StartAsTask 
            :> Task
        )

    let handleLogMessage (logMessage: LogMessage) = async { 
        let now = DateTimeOffset.UtcNow.ToString()
        printfn "[%s] %s: %s" now logMessage.Source logMessage.Message
    }

    do 
        singleArgFunc handleLogMessage
        |> client.add_Log

        let connectTask =
            async {
                do! client.LoginAsync(TokenType.Bot, token) |> Async.AwaitTask
                do! client.StartAsync() |> Async.AwaitTask
                do! client.SetGameAsync "This Is Legal But We Question The Ethics" |> Async.AwaitTask

                // Trip the ready event once the client indicates it's ready
                let func = Func<Task>(fun _ -> readyEvent.Set() |> ignore; Task.CompletedTask)
                client.add_Ready func 
            }
            
        Async.RunSynchronously connectTask

    interface IAsyncDisposable with
        member _.DisposeAsync () =
            logger.LogWarning("The bot is disposing")
            readyEvent.Dispose()
            client.StopAsync()
            |> ValueTask

    member _.GetChannel (channelId : int64) = 
        client.GetChannel (uint64 channelId)

    member _.ListGuildsAsync () = 
        client.Rest.GetGuildsAsync()
    
    member _.UpdateGameAsync message =
        client.SetGameAsync(message)

    member x.PostAffixesMessageAsync (_ : GuildId) (channelId : int64) (affixes: RaiderIo.ListAffixesResponse) =
        async {
            // Wait for the bot's ready event to fire. If the bot is not yet ready, the channel will be null
            readyEvent.WaitOne() |> ignore
            
            sprintf "Posting affixes to channel %i" channelId 
            |> logger.LogInformation
            
            let channel = x.GetChannel channelId
            
            return! messageHandler.SendAffixesMessage (channel :> IChannel :?> IMessageChannel) affixes
        }

    member _.AddEventListener eventType =

        match eventType with
        | MessageReceived fn ->
            singleArgFunc fn
            |> client.add_MessageReceived 
        | UserLeft fn ->
            singleArgFunc fn
            |> client.add_UserLeft
        | UserJoined fn ->
            singleArgFunc fn
            |> client.add_UserJoined
        | UserUpdated fn ->
            doubleArgFunc fn
            |> client.add_GuildMemberUpdated
        | BotLeftGuild fn ->
            singleArgFunc fn
            |> client.add_LeftGuild

        Async.Empty
