namespace Dijon.Bot

open Dijon.Shared

open System
open System.Threading
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Options

type CachedUserMessage = Cacheable<IUserMessage, uint64>

type DiscordEvent =
    | UserLeft of (SocketGuildUser -> Task<unit>)
    | UserJoined of (SocketGuildUser -> Task<unit>)
    | UserUpdated of (SocketGuildUser -> SocketGuildUser -> Task<unit>)
    | BotLeftGuild of (SocketGuild -> Task<unit>)
    | CommandReceived of (IMessage -> Command -> Task<unit>)
    | ReactionReceived of (CachedUserMessage -> IChannel -> IReaction -> Task<unit>)

type IBotClient =
    abstract member InitAsync: cancellationToken: CancellationToken -> Task<unit>
    abstract member GetChannel: channelId: int64 -> SocketChannel
    abstract member ListGuildsAsync: unit -> Task<Collections.Generic.IReadOnlyCollection<IGuild>>
    abstract member SetActivityStatusAsync: message: string -> Task
    abstract member GetLatency: unit -> int64;
    abstract member GetBotUserId: unit -> uint64;
    abstract member RemoveAllReactionsForEmoteAsync: channelId: uint64 * msgId: uint64 * emote: IEmote -> Task
    abstract member AddEventListener: eventType: DiscordEvent -> unit

type BotClient(
    options: IOptions<BotClientOptions>,
    logger: ILogger<BotClient>
) =
    let client = new DiscordSocketClient()
    let readyEvent = new System.Threading.ManualResetEvent false
    let token = options.Value.ApiToken

    let singleArgFunc (fn : 'a -> Task<unit>) =
        // Transform the F# function to a C# Func<'a, Task>
        Func<'a, Task>(fun arg ->
            // Don't call the handler until we know the socket client is ready
            readyEvent.WaitOne()
            |> ignore

            let task = task {
                match! Task.catch (fn arg) with
                | Choice1Of2 _ ->
                    ()
                | Choice2Of2 err ->
                    logger.LogError(err, "Single arg event listener failed: {0}", err.Message)
            }

            task :> Task
        )

    let doubleArgFunc (fn : 'a -> 'b -> Task<unit>) =
        // Transform the F# function to a C# Func<'a, 'b', Task>
        Func<'a, 'b, Task>(fun a b ->
            // Don't call the handler until we know the socket client is ready
            readyEvent.WaitOne()
            |> ignore

            let task = task {
                match! Task.catch (fn a b) with
                | Choice1Of2 _ ->
                    ()
                | Choice2Of2 err ->
                    logger.LogError(err, "Double arg event listener failed: {0}", err.Message)
            }

            task :> Task
        )

    let tripleArgFunc (fn : 'a -> 'b -> 'c -> Task<unit>) =
        // Transform the F# function to a C# Func<'a, 'b', Task>
        Func<'a, 'b, 'c, Task>(fun a b c ->
            // Don't call the handler until we know the socket client is ready
            readyEvent.WaitOne()
            |> ignore

            let task = task {
                match! Task.catch (fn a b c) with
                | Choice1Of2 _ ->
                    ()
                | Choice2Of2 err ->
                    logger.LogError(err, "Triple arg event listener failed: {0}", err.Message)
            }

            task :> Task
        )

    let handleLogMessage (logMessage: LogMessage) =
        let now = DateTimeOffset.UtcNow.ToString()
        printfn $"[%s{now}] %s{logMessage.Source}: %s{logMessage.Message}"

    let delegateCommandMessages (fn : IMessage -> Command -> Task<unit>) (msg : IMessage) =
        match CommandParser.ParseCommand msg with
        | Ignore ->
            Task.empty
        | cmd ->
            task {
                match! Task.catch (fn msg cmd) with
                | Choice1Of2 _ ->
                    ()
                | Choice2Of2 err ->
                    logger.LogError(err, $"Command message delegate failed to handle command %A{cmd}")
            }

    interface IAsyncDisposable with
        member _.DisposeAsync () =
            logger.LogWarning("Something attempted to dispose the bot.")
            readyEvent.Dispose()
            ValueTask.CompletedTask
            //client.StopAsync()
            //|> ValueTask
    end

    interface IBotClient with
        member _.InitAsync cancellationToken =
            task {
                // Trip the ready event once the client indicates it's ready
                let func = Func<Task>(fun _ -> readyEvent.Set() |> ignore; Task.CompletedTask)
                client.add_Ready func

                do! client.LoginAsync(TokenType.Bot, token)
                do! client.StartAsync()
                do! client.SetGameAsync "This Is Legal But We Question The Ethics"

                readyEvent.WaitOne()
                |> ignore
            }

        member _.GetChannel (channelId : int64) =
            client.GetChannel (uint64 channelId)

        member _.ListGuildsAsync () =
            let client = client :> IDiscordClient
            client.GetGuildsAsync(CacheMode.CacheOnly, RequestOptions.Default)

        member _.SetActivityStatusAsync message =
            client.SetGameAsync(message)

        member _.GetLatency () =
            client.Latency

        member _.GetBotUserId () =
            client.CurrentUser.Id

        member _.RemoveAllReactionsForEmoteAsync (channelId: uint64, msgId: uint64, emote: IEmote) =
            client.Rest.RemoveAllReactionsForEmoteAsync(channelId, msgId, emote)

        member _.AddEventListener eventType =
            match eventType with
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
            | ReactionReceived fn ->
                tripleArgFunc fn
                |> client.add_ReactionAdded
        end
