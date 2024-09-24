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

type CachedGuildUser = Cacheable<SocketGuildUser, uint64>
type CachedUser = Cacheable<IUser, uint64>
type CachedUserMessage = Cacheable<IUserMessage, uint64>
type CachedChannel = Cacheable<IMessageChannel, uint64>

type DiscordEvent =
    | UserLeft of (SocketGuild -> SocketUser -> Task<unit>)
    | UserUpdated of (CachedGuildUser -> SocketGuildUser -> Task<unit>)
    | BotLeftGuild of (SocketGuild -> Task<unit>)
    | CommandReceived of (IMessage -> Command -> Task<unit>)
    | ReactionReceived of (CachedUserMessage -> CachedChannel -> IReaction -> Task<unit>)
    | UserIsTyping of (CachedUser -> CachedChannel -> Task<unit>)
    | SlashCommandExecuted of (SocketSlashCommand -> Task<unit>)

type IBotClient =
    abstract member InitAsync: cancellationToken: CancellationToken -> Task<unit>
    abstract member GetChannel: channelId: int64 -> SocketChannel
    abstract member ListGuildsAsync: unit -> Task<Collections.Generic.IReadOnlyCollection<IGuild>>
    abstract member SetActivityStatusAsync: message: string -> Task
    abstract member GetLatency: unit -> int64;
    abstract member GetBotUserId: unit -> uint64;
    abstract member RemoveAllReactionsForEmoteAsync: channelId: uint64 * msgId: uint64 * emote: IEmote -> Task
    abstract member AddEventListener: eventType: DiscordEvent -> unit
    abstract member RegisterCommands: commands: ApplicationCommandProperties seq -> Task

type BotClient(
    options: IOptions<BotClientOptions>,
    logger: ILogger<BotClient>
) =
    let client = new DiscordSocketClient(DiscordSocketConfig( GatewayIntents = GatewayIntents.All ))
    let readyEvent = new ManualResetEvent false
    let token = options.Value.ApiToken

    let singleArgFunc (fn : 'a -> Task<unit>) =
        // Transform the F# function to a C# Func<'a, Task>
        Func<'a, Task>(fun arg ->
            // Don't call the handler until we know the socket client is ready
            readyEvent.WaitOne()
            |> ignore

            task {
                match! Task.catch (fn arg) with
                | Choice1Of2 _ ->
                    ()
                | Choice2Of2 err ->
                    logger.LogError(err, "Single arg event listener failed: {0}", err.Message)
            }
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

    /// Delegates command messages and runs them off the main thread, so that they don't block the socket client's gateway task.
    let delegateCommandMessages (fn : IMessage -> Command -> Task<unit>) (msg : IMessage) =
        // Don't call the handler until we know the socket client is ready
        readyEvent.WaitOne()
        |> ignore
        // Parse the message and invoke the handler off the main thread
        Task.start(task {
            match CommandParser.ParseCommand msg with
            | Ignore ->
                ()
            | cmd ->
                match! Task.catch (fn msg cmd) with
                | Choice1Of2 _ ->
                    ()
                | Choice2Of2 err ->
                    logger.LogError(err, $"Command message delegate failed to handle command %A{cmd}")
        })
        Task.CompletedTask

    let handleLogMessage (logMessage: LogMessage) =
        let level =
            match logMessage.Severity with
            | LogSeverity.Critical -> LogLevel.Critical
            | LogSeverity.Error -> LogLevel.Error
            | LogSeverity.Warning -> LogLevel.Warning
            | LogSeverity.Info -> LogLevel.Information
            | LogSeverity.Verbose -> LogLevel.Trace
            | LogSeverity.Debug -> LogLevel.Debug
            | _ -> ArgumentOutOfRangeException(nameof logMessage.Severity) |> raise
        if isNull logMessage.Exception
        then logger.Log(level, logMessage.Message, [| logMessage.Source |])
        else logger.Log(level, logMessage.Exception, logMessage.Message, [| logMessage.Source |])
        Task.CompletedTask

    let connect () =
        task {
            logger.LogInformation("Bot is connecting")
            do! client.LoginAsync(TokenType.Bot, token)
            do! client.StartAsync()
            do! client.SetGameAsync "This Is Legal But We Question The Ethics"
        }

    let handleBotDisconnected (ex: exn) =
        logger.LogError(ex, "Bot is disconnecting with exception")
        Task.CompletedTask

    let handleReadyEvent () =
        readyEvent.Set()
        |> ignore
        Task.CompletedTask

    interface IAsyncDisposable with
        member _.DisposeAsync () =
            logger.LogWarning("Something attempted to dispose the bot.")
            readyEvent.Dispose()
            ValueTask.CompletedTask
            //client.StopAsync()
            //|> ValueTask
    end

    interface IBotClient with
        member _.InitAsync _ =
            task {
                // Trip the ready event once the client indicates it's ready
                client.add_Ready handleReadyEvent
                client.add_Disconnected handleBotDisconnected
                client.add_Log handleLogMessage

                do! connect()

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
                doubleArgFunc fn
                |> client.add_UserLeft
            | UserUpdated fn ->
                doubleArgFunc fn
                |> client.add_GuildMemberUpdated
            | BotLeftGuild fn ->
                singleArgFunc fn
                |> client.add_LeftGuild
            | CommandReceived fn ->
                delegateCommandMessages fn
                |> client.add_MessageReceived
            | ReactionReceived fn ->
                tripleArgFunc fn
                |> client.add_ReactionAdded
            | UserIsTyping fn ->
                doubleArgFunc fn
                |> client.add_UserIsTyping
            | SlashCommandExecuted fn ->
                singleArgFunc fn
                |> client.add_SlashCommandExecuted

        member _.RegisterCommands commands =
            task {
                for command in commands do
                    let! createdCommand = client.CreateGlobalApplicationCommandAsync(command)
                    logger.LogInformation("Created global application command \"{CommandName}\" with id {CommandId}", createdCommand.Name, createdCommand.Id)
            }
        end
