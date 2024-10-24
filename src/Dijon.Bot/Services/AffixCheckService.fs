namespace Dijon.Bot.Services

open Dijon.Database
open Dijon.Database.AffixChannels
open Dijon.Bot
open Dijon.Bot.RaiderIo
open Dijon.Shared

open System
open System.Threading.Tasks
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Cronos
open TimeZoneConverter
open Discord
open Discord.WebSocket

type private NextSchedule =
    | FromTimeSpan of TimeSpan
    | FromCron

type AffixCheckService(logger : ILogger<AffixCheckService>,
                       bot : IBotClient,
                       database : IAffixChannelsDatabase) =

    let [<Literal>] StopAffixesCommandName = "stopaffixes"

    let mutable timer : System.Timers.Timer option = None

    // Every Tuesday at 9am
    let schedule = CronExpression.Parse("0 9 * * 2")

    // Central Standard Time (automatically handles DST)
    // Use the TimeZoneConverter package to convert between Windows/Linux/Mac TimeZone names
    // https://devblogs.microsoft.com/dotnet/cross-platform-time-zones-with-net-core/
    let timezone = TZConvert.GetTimeZoneInfo "America/Chicago"

    let createAffixesEmbed (affixes: ListAffixesResponse) =
        let builder = EmbedBuilder()
        builder.Color <- Nullable Color.Green
        builder.Title <- sprintf "This week's Mythic+ affixes: %s" affixes.title

        affixes.affix_details
        |> List.map (fun affix -> MessageUtils.embedField (sprintf "**%s**" affix.name) affix.description)
        |> builder.Fields.AddRange

        builder

    let postAffixesMessageAsync (_ : GuildId) (channelId : int64) (affixes: RaiderIo.ListAffixesResponse) =
        task {
            // TODO: why is this commented out? we aren't we waiting here?
            // Wait for the bot's ready event to fire. If the bot is not yet ready, the channel will be null
            //readyEvent.WaitOne() |> ignore

            $"Posting affixes to channel %i{channelId}"
            |> logger.LogInformation

            let channel = bot.GetChannel channelId
            let embed = createAffixesEmbed affixes

            do! MessageUtils.sendEmbed (channel :> IChannel :?> IMessageChannel) embed
                |> Task.ignore
        }

    let postAffixes (affixes : RaiderIo.ListAffixesResponse) channels =
        let rec post hasPosted channels =
            match channels with
            | [] ->
                Task.wrap hasPosted
            | channel :: remaining ->
                // Only post this message if it has not been posted to the guild/channel
                match channel.LastAffixesPosted with
                | Some title when title = affixes.title ->
                    post hasPosted remaining
                | _ ->
                    let guildId = GuildId channel.GuildId

                    task {
                        do! postAffixesMessageAsync guildId channel.ChannelId affixes
                        do! database.SetLastAffixesPostedForGuild guildId affixes.title
                        // Post to the remaining channels
                        return! post true remaining
                    }

        post false channels

    let checkAffixes _ : Task<bool> =
        task {
            let! channels = database.ListAllAffixChannels()

            if List.isEmpty channels then
                logger.LogInformation("No guilds have enabled the affixes channel, no reason to check affixes")
                return true
            else
                match! Affixes.list() with
                | Error err ->
                    logger.LogError $"Failed to get new affixes due to reason: %s{err}"
                    return false
                | Ok affixes ->
                    logger.LogInformation $"Got affixes: %s{affixes.title}"
                    return! postAffixes affixes channels
        }

    let rec scheduleJob (cancellation : CancellationToken) nextSchedule =
        let now = DateTimeOffset.Now
        let delay =
            match nextSchedule with
            | FromCron ->
                schedule.GetNextOccurrence(now, timezone)
                |> Option.ofNullable
                |> Option.map (fun next -> next - now)
            | FromTimeSpan ts ->
                Some ts

        match delay with
        | None ->
            ()
        | Some delay ->
            let baseTimer = new System.Timers.Timer(delay.TotalMilliseconds)
            // Set AutoReset to false so the event is only raised once per timer
            baseTimer.AutoReset <- false
            timer <- Some baseTimer

            baseTimer.Elapsed
            |> Event.add (fun _ ->
                baseTimer.Dispose()
                timer <- None

                if not cancellation.IsCancellationRequested then
                    let hasPosted =
                        checkAffixes ()
                        |> Task.runSynchronously

                    // Schedule the next job
                    if hasPosted then
                        // Affixes were posted, which means the bot can wait until the next cron schedule.
                        scheduleJob cancellation FromCron
                    else
                        // Affixes were not posted, meaning they have not been updated yet. Check again in 5 minutes.
                        TimeSpan.FromMinutes 5.
                        |> FromTimeSpan
                        |> scheduleJob cancellation
            )

            logger.LogInformation(
                "Next affix check occurs at {0} ({1:f1} hours from now)",
                now + delay,
                delay.TotalHours
            )
            baseTimer.Start()

    let handleSetAffixesChannelCommand (msg: IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            if msg.Author.Id <> KnownUsers.DjurId then
                let message =
                    "At the moment, only "
                    + MentionUtils.MentionUser KnownUsers.DjurId
                    + " may set the affxes channel."

                MessageUtils.sendMessage msg.Channel message
            else
                task {
                    let guildId = GuildId (int64 guildChannel.Guild.Id)
                    do! database.SetAffixesChannelForGuild guildId (int64 msg.Channel.Id)
                    do! MessageUtils.sendMessage msg.Channel "Affixes will be sent to this channel every Tuesday."
                }
        | :? ISocketPrivateChannel ->
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in a private message."
        | _ ->
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in unknown channel type."

    let handleGetAffixesCommand (msg : IMessage) =
        task {
            let! editable = MessageUtils.sendEditableMessage msg.Channel "Fetching affixes, please wait..."
            let! affixList = Affixes.list()
            let editMessage (props : MessageProperties) =
                let embed =
                    let builder = EmbedBuilder()
                    match affixList with
                    | Error err ->
                        builder.Color <- Nullable Color.Red
                        builder.Title <- "❌ Error fetching affixes!"

                        MessageUtils.embedField "🔬 Details" err
                        |> builder.Fields.Add

                        builder.Build()
                    | Ok affixes ->
                        let embed = createAffixesEmbed affixes
                        embed.Build()

                // Clear the content and add an embed
                props.Content <- Optional.Create ""
                props.Embed <- Optional.Create embed

            do! editable.ModifyAsync (Action<MessageProperties> editMessage)

            match msg.Channel, affixList with
            | :? IGuildChannel as channel, Ok affixes ->
                // Save this list of affixes as the guild's latest seen version
                do! database.SetLastAffixesPostedForGuild (GuildId <| int64 channel.GuildId) affixes.title
                logger.LogInformation("Updated latest affixes for guild {0} to {1}", channel.Guild.Name, affixes.title)
            | _ ->
                ()
        }

    let handleCommandMessage (msg : IMessage) = function
        | SetAffixesChannel -> handleSetAffixesChannelCommand msg
        | GetAffix -> handleGetAffixesCommand msg
        | _ -> Task.empty

    let handleRemoveAffixesCommand (command: SocketSlashCommand) =
        task {
            match command.Channel with
            | :? SocketGuildChannel as guildChannel ->
                if command.User.Id <> KnownUsers.DjurId then
                    let message =
                        "At the moment, only "
                        + MentionUtils.MentionUser KnownUsers.DjurId
                        + " may set the affxes channel."
                    MessageUtils.sendMessage command.Channel message
                else
                    task {
                        let guildId = GuildId (int64 guildChannel.Guild.Id)
                        do! database.SetAffixesChannelForGuild guildId (int64 command.Channel.Id)
                        do! MessageUtils.sendMessage command.Channel "Affixes will be sent to this channel every Tuesday."
                    }
            | :? ISocketPrivateChannel ->
                MessageUtils.sendMessage command.Channel "Unable to set log channel in a private message."
            | x ->
                MessageUtils.sendMessage x "Unable to set log channel in unknown channel type."
                return failwith "nyi"
        }

    let handleSlashCommandExecuted (command: SocketSlashCommand) =
        match command.CommandName with
        | StopAffixesCommandName -> handleRemoveAffixesCommand command
        | _ -> Task.empty

    let buildRemoveAffixesChannelCommand () =
        SlashCommandBuilder()
            .WithName(StopAffixesCommandName)
            .WithDescription("Turn off Dijon's weekly M+ Affixes check. You can still use the /affixes command to check M+ Affixes on demand.")
            .WithContextTypes(InteractionContextType.Guild)
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
            .Build()

    interface IDisposable with
        member _.Dispose() =
            timer
            |> Option.iter (fun timer -> timer.Dispose())

    interface IHostedService with
        member _.StartAsync cancellationToken =
            task {
                let! _ = checkAffixes ()
                bot.AddEventListener (DiscordEvent.CommandReceived handleCommandMessage)
                bot.AddEventListener (DiscordEvent.SlashCommandExecuted handleSlashCommandExecuted)
                do! bot.RegisterCommands [buildRemoveAffixesChannelCommand ()]

                scheduleJob cancellationToken FromCron
            }

        member _.StopAsync cancellationToken =
            timer
            |> Option.iter (fun timer -> timer.Stop())

            Task.CompletedTask
