namespace Dijon.Bot

open Dijon.Shared

open System
open System.Threading
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open Microsoft.Extensions.Logging
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
    abstract member GetConnectionState: unit -> ConnectionState
    abstract member GetBotUserId: unit -> uint64;
    abstract member RemoveAllReactionsForEmoteAsync: channelId: uint64 * msgId: uint64 * emote: IEmote -> Task
    abstract member AddEventListener: eventType: DiscordEvent -> unit
    abstract member RegisterCommands: commands: ApplicationCommandProperties seq -> Task

type BotClient(
    options: IOptions<BotClientOptions>,
    logger: ILogger<BotClient>
) =
    let config = DiscordSocketConfig()
    do
        config.GatewayIntents <- enum<GatewayIntents> (int GatewayIntents.Guilds ||| int GatewayIntents.GuildMembers ||| int GatewayIntents.GuildMessages ||| int GatewayIntents.GuildMessageReactions ||| int GatewayIntents.MessageContent)
        config.AlwaysDownloadUsers <- true
    let client = new DiscordSocketClient(config)
    let readySignal = Event<unit>()
    let token = options.Value.ApiToken

    let withErrorHandling (eventName: string) (job: Task<unit>) : Task =
        task {
            match! Task.catch job with
            | Choice1Of2 _ -> ()
            | Choice2Of2 err -> logger.LogError(err, "{EventName} handler failed: {0}", eventName, err.Message)
        } :> Task

    /// Delegates command messages and runs them off the main thread, so that they don't block the socket client's gateway task.
    let delegateCommandMessages (fn : IMessage -> Command -> Task<unit>) (msg : IMessage) =
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
        // Discord.Net's ConnectionManager handles reconnection automatically with exponential backoff
        // (1s initial, doubling with jitter, capped at 60s). This handler is for diagnostics only.
        let connectionState = client.ConnectionState
        if connectionState = ConnectionState.Connecting then
            logger.LogWarning(ex, "Bot disconnected; reconnecting automatically (state: {State})", connectionState)
        else
            logger.LogError(ex, "Bot disconnected (state: {State})", connectionState)
        Task.CompletedTask

    let handleReadyEvent () =
        logger.LogInformation("Bot received ready event from Discord, triggering ready signal")
        readySignal.Trigger(())
        Task.CompletedTask

    interface IAsyncDisposable with
        member _.DisposeAsync () =
            logger.LogWarning("Something attempted to dispose the bot.")
            ValueTask.CompletedTask
    end

    interface IBotClient with
        member _.InitAsync _ =
            task {
                client.add_Ready handleReadyEvent
                client.add_Disconnected handleBotDisconnected
                client.add_Log handleLogMessage

                // Start awaiting the ready signal before connecting so the subscription is
                // registered before Discord can fire the Ready event.
                let awaitReady = Async.AwaitEvent readySignal.Publish |> Async.StartAsTask

                do! connect()

                logger.LogInformation("Waiting for bot ready signal")
                do! awaitReady
                logger.LogInformation("Bot ready signal received")
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

        member _.GetConnectionState () =
            client.ConnectionState

        member _.GetBotUserId () =
            client.CurrentUser.Id

        member _.RemoveAllReactionsForEmoteAsync (channelId: uint64, msgId: uint64, emote: IEmote) =
            client.Rest.RemoveAllReactionsForEmoteAsync(channelId, msgId, emote)

        member _.AddEventListener eventType =
            match eventType with
            | UserLeft fn ->
                client.add_UserLeft(Func<_, _, Task>(fun guild user -> withErrorHandling "UserLeft" (fn guild user)))
            | UserUpdated fn ->
                client.add_GuildMemberUpdated(Func<_, _, Task>(fun a b -> withErrorHandling "UserUpdated" (fn a b)))
            | BotLeftGuild fn ->
                client.add_LeftGuild(Func<_, Task>(fun guild -> withErrorHandling "BotLeftGuild" (fn guild)))
            | CommandReceived fn ->
                client.add_MessageReceived(Func<_, Task>(delegateCommandMessages fn))
            | ReactionReceived fn ->
                client.add_ReactionAdded(Func<_, _, _, Task>(fun msg ch reaction -> withErrorHandling "ReactionReceived" (fn msg ch reaction)))
            | UserIsTyping fn ->
                client.add_UserIsTyping(Func<_, _, Task>(fun user ch -> withErrorHandling "UserIsTyping" (fn user ch)))
            | SlashCommandExecuted fn ->
                client.add_SlashCommandExecuted(Func<_, Task>(fun cmd -> withErrorHandling "SlashCommandExecuted" (fn cmd)))

        member _.RegisterCommands commands =
            task {
                for command in commands do
                    let! createdCommand = client.CreateGlobalApplicationCommandAsync(command)
                    logger.LogInformation("Created global application command \"{CommandName}\" with id {CommandId}", createdCommand.Name, createdCommand.Id)
            }
        end
