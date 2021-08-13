namespace Dijon

open System
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive

type DiscordEvent = 
    | MessageReceived of (IMessage -> Async<unit>)
    | UserLeft of (SocketGuildUser -> Async<unit>)
    | UserJoined of (SocketGuildUser -> Async<unit>)
    | UserUpdated of (SocketGuildUser -> SocketGuildUser -> Async<unit>)
    | BotLeftGuild of (SocketGuild -> Async<unit>)
    | CommandReceived of (IMessage -> Command -> Async<unit>)

type BotClient(logger : ILogger<BotClient>, config : IConfiguration) =
    let client = new DiscordSocketClient()
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

    let delegateCommandMessages (fn : IMessage -> Command -> Async<unit>) (msg : IMessage) = 
        match CommandParser.ParseCommand msg with
        | Ignore -> 
            Async.Empty
        | cmd -> 
            async {
                match! fn msg cmd |> Async.Catch with
                | Choice1Of2 _ -> 
                    ()
                | Choice2Of2 err ->
                    logger.LogError(err, sprintf "Command message delegate failed to handle command %A" cmd)
            }

    interface IAsyncDisposable with
        member _.DisposeAsync () =
            logger.LogWarning("The bot is disposing")
            readyEvent.Dispose()
            client.StopAsync()
            |> ValueTask

    member _.InitAsync () = 
        task {
            do! client.LoginAsync(TokenType.Bot, token) 
            do! client.StartAsync() 
            do! client.SetGameAsync "This Is Legal But We Question The Ethics" 

            // Trip the ready event once the client indicates it's ready
            let func = Func<Task>(fun _ -> readyEvent.Set() |> ignore; Task.CompletedTask)
            client.add_Ready func 

            readyEvent.WaitOne()
            |> ignore
        }

    member _.GetChannel (channelId : int64) = 
        client.GetChannel (uint64 channelId)

    member _.ListGuildsAsync () = 
        let client = client :> IDiscordClient
        client.GetGuildsAsync(CacheMode.CacheOnly, RequestOptions.Default)
    
    member _.UpdateGameAsync message =
        client.SetGameAsync(message)

    member _.GetLatency () = 
        client.Latency

    member _.GetBotUserId () = 
        client.CurrentUser.Id

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
        | CommandReceived fn ->
            singleArgFunc (delegateCommandMessages fn)
            |> client.add_MessageReceived

        Async.Empty
