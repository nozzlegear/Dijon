namespace Dijon.Services

open System
open System.Threading.Tasks
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Dijon
open Dijon.RaiderIo
open Cronos
open TimeZoneConverter
open Discord
open Discord.WebSocket
open FSharp.Control.Tasks.V2.ContextInsensitive

type private NextSchedule = 
    | FromTimeSpan of TimeSpan
    | FromCron

type AffixCheckService(logger : ILogger<AffixCheckService>, 
                       bot : Dijon.BotClient, 
                       database : IDijonDatabase) =

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
        async {
            // Wait for the bot's ready event to fire. If the bot is not yet ready, the channel will be null
            //readyEvent.WaitOne() |> ignore
            
            sprintf "Posting affixes to channel %i" channelId 
            |> logger.LogInformation
            
            let channel = bot.GetChannel channelId
            let embed = createAffixesEmbed affixes
            
            do! MessageUtils.sendEmbed (channel :> IChannel :?> IMessageChannel) embed 
                |> Async.Ignore
        }
    
    let postAffixes (affixes : RaiderIo.ListAffixesResponse) channels =
        let rec post hasPosted channels = 
            match channels with
            | [] ->
                Async.Wrap hasPosted
            | channel :: remaining ->
                // Only post this message if it has not been posted to the guild/channel
                match channel.LastAffixesPosted with
                | Some title when title = affixes.title ->
                    post hasPosted remaining
                | _ ->
                    let guildId = GuildId channel.GuildId
                    
                    async {
                        do! postAffixesMessageAsync guildId channel.ChannelId affixes
                        do! database.SetLastAffixesPostedForGuild guildId affixes.title
                        // Post to the remaining channels
                        return! post true remaining
                    }

        post false channels
    
    let checkAffixes _ : Async<bool> =
        async {
            let! channels = database.ListAllAffixChannels()
            
            if List.isEmpty channels then
                logger.LogInformation(sprintf "No guilds have enabled the affixes channel, no reason to check affixes")
                return true
            else
                match! Affixes.list() with
                | Error err ->
                    logger.LogError(sprintf "Failed to get new affixes due to reason: %s" err)
                    return false
                | Ok affixes ->
                    logger.LogInformation(sprintf "Got affixes: %s" affixes.title)
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
                        |> Async.RunSynchronously
                    
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
                async {
                    let guildId = GuildId (int64 guildChannel.Guild.Id)
                    
                    do! database.SetAffixesChannelForGuild guildId (int64 msg.Channel.Id)
                    do! MessageUtils.sendMessage msg.Channel "Affixes will be sent to this channel every Tuesday."
                }
        | :? ISocketPrivateChannel ->
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in a private message."
        | _ ->
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in unknown channel type."
            
    let handleGetAffixesCommand (msg : IMessage) =
        async {
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
                    
            do! editable.ModifyAsync (Action<MessageProperties> editMessage) |> Async.AwaitTask
        }

    let commandReceived (msg : IMessage) = function
        | SetAffixesChannel -> handleSetAffixesChannelCommand msg
        | GetAffix -> handleGetAffixesCommand msg
        | _ -> Async.Empty
    
    interface IDisposable with
        member _.Dispose() =
            timer
            |> Option.iter (fun timer -> timer.Dispose())

    interface IHostedService with
        member _.StartAsync cancellationToken =
            task {
                do! checkAffixes () 
                    |> Async.Ignore
                do! bot.AddEventListener (DiscordEvent.CommandReceived commandReceived)

                scheduleJob cancellationToken FromCron
            }
            :> Task
            
        member _.StopAsync cancellationToken =
            timer
            |> Option.iter (fun timer -> timer.Stop())
            
            Task.CompletedTask
