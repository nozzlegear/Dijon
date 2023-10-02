namespace Dijon.Bot.Services

open Dijon.Bot
open Dijon.Database
open Dijon.Database.StreamAnnouncements
open Dijon.Shared

open Discord
open Discord.WebSocket
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System
open System.Threading.Tasks

type StreamCheckService(logger : ILogger<StreamCheckService>, 
                        database : IStreamAnnouncementsDatabase,
                        bot : IBotClient) =
            
    /// Tries to get the user's stream activity. Returns None if the user is not streaming.
    let tryGetStreamActivity (user : IGuildUser): Option<StreamingGame> = 
        user.Activities
        |> Seq.tryPick (function
            | :? StreamingGame as stream -> Some stream 
            | _ -> None)

    /// Checks if the user's activities indicate they're streaming. 
    let isStreaming(user : IGuildUser): bool = 
        tryGetStreamActivity user
        |> Option.isSome

    let getTwitchUsernameFromUrl (url : string) =
        let uri = Uri url

        // Taking the following URL: https://twitch.tv/nozzlegear
        // uri.Segments will give an array with two entries:
        // [| "/"; "nozzlegear" |]
        //
        // With this URL: https://twitch.tv/nozzlegear/something
        // uri.Segments will give an array with three entries:
        // [| "/"; "nozzlegear/"; "something" |]
        // 
        // Note the trailing slash in "nozzlegear/"!
        
        match Seq.tryItem 1 uri.Segments with
        | Some segment ->
            segment.Trim '/'
        | None ->
            // Unlikely, but item 1 could not be found. Just return the last segment.
            Seq.last uri.Segments

    let buildStreamAnnouncementEmbed (stream : StreamData) =
        let twitchUsername = getTwitchUsernameFromUrl stream.Url
        let discordNickname = MessageUtils.GetNickname stream.User
        let embed = EmbedBuilder()
        embed.Title <- sprintf "%s is live on %s right now!" discordNickname stream.Name
        embed.Color <- Nullable Color.Green
        embed.Description <- stream.Details
        embed.ThumbnailUrl <- stream.User.GetAvatarUrl(size = uint16 256)
        embed.ImageUrl <- sprintf "https://static-cdn.jtvnw.net/previews-ttv/live_user_%s-1280x740.jpg" twitchUsername
        embed.Url <- stream.Url
        embed.AddField (fun builder ->  
                builder.Name <- "Stream URL"
                builder.Value <- stream.Url)

    let sendStreamAnnouncementMessage (stream : StreamData) =
        logger.LogInformation("Announcing stream for user {0} ({1})", stream.User.Username, stream.User.Id)

        task {
            match! database.GetStreamAnnouncementChannelForGuild (GuildId stream.GuildId) with
            | Some channelData ->
                let channel = bot.GetChannel channelData.ChannelId
                let! messageId = 
                    buildStreamAnnouncementEmbed stream
                    |> MessageUtils.sendEmbed (channel :> IChannel :?> IMessageChannel)
                let announcementMessage =
                    { ChannelId = channelData.ChannelId
                      MessageId = messageId
                      GuildId = stream.GuildId
                      StreamerId = int64 stream.User.Id }

                do! database.AddStreamAnnouncementMessage announcementMessage

                return Ok messageId
            | None ->
                return Error "Guild does not have a stream announcements channel set."
        }

    let removeStreamingMessage (user : IUser) : Task<unit> =
        let userId = int64 user.Id

        logger.LogInformation("Removing streaming message for user {0} ({1})", user.Username, userId)

        task {
            let options = 
                let o = RequestOptions.Default
                o.AuditLogReason <- "Stream ended"
                o
            let! messages = database.ListStreamAnnouncementMessagesForStreamer userId
            let totalMessages = Seq.length messages

            if totalMessages > 5 then
                logger.LogWarning("User {0} ({1}) has {2} stream announcement messages. Only deleting the first 5.", user.Username, userId, totalMessages)

            // Delete each message
            for message in Seq.truncate 5 messages do
                let channel = bot.GetChannel message.ChannelId :> IChannel :?> IMessageChannel
                let deletion = channel.DeleteMessageAsync(uint64 message.MessageId, options)
                               |> Task.catch

                match! deletion with
                | Choice2Of2 (:? Discord.Net.HttpException as ex) when int ex.HttpCode = 404 ->
                    // The message was already deleted, possibly by hand
                    ()
                | Choice2Of2 err ->
                    logger.LogError(err, "Failed to delete stream announcement message for user {0} ({1})", user.Username, userId)
                | _ ->
                    ()

            // Delete the database messages for the streamer
            do! database.DeleteStreamAnnouncementMessageForStreamer userId
        }

    let handleTestStreamStartedCommand (msg : IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            task {
                let streamData =
                    { Name = "Twitch"
                      Details = "An amazing test stream that's not actually live right now! It's a test!"
                      Url = "https://twitch.tv/nozzlegear"
                      User = msg.Author
                      GuildId = int64 guildChannel.Guild.Id }

                match! sendStreamAnnouncementMessage streamData with
                | Ok _ ->
                    return! MessageUtils.AddGreenCheckReaction msg
                | Error err ->
                    return! sprintf "Error: %s" err
                            |> MessageUtils.sendMessage msg.Channel
            }
        | :? ISocketPrivateChannel ->
            MessageUtils.sendMessage msg.Channel "Command is not supported in a private message."
        | _ ->
            MessageUtils.sendMessage msg.Channel "Command is not supported in unknown channel type."

    let handleTestStreamEndedCommand (msg : IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel ->
            task {
                do! removeStreamingMessage msg.Author

                return! MessageUtils.AddGreenCheckReaction msg
            }
        | _ ->
            MessageUtils.sendMessage msg.Channel "Command is not supported in unknown channel type."

    let userUpdated (before : SocketGuildUser) (after : SocketGuildUser) = 
        task {
            let wasStreaming = isStreaming before
            let stream = tryGetStreamActivity after

            match wasStreaming, stream with
            | true, None ->
                // Always try to remove streaming messages for the user even if they don't currenty have a streamer role.
                // This covers cases where they had a streamer role but it was removed while they were streaming.
                return! removeStreamingMessage after
            | false, Some stream ->
                // Fetch all streamer role IDs and then check if the user has one
                let! allRoles = 
                    database.ListStreamerRoles ()
                let userRoles = 
                    after.Roles
                    |> Seq.map (fun r -> int64 r.Id)
                    |> Set.ofSeq
                let hasStreamerRole = 
                    allRoles
                    |> Set.exists (fun role -> Set.contains role userRoles)

                if hasStreamerRole then
                    // User is streaming and they have the streaming role. Announce the stream.
                    let streamData = 
                        { User = after
                          Name = stream.Name
                          Details = stream.Details
                          Url = stream.Url
                          GuildId = int64 after.Guild.Id }

                    match! sendStreamAnnouncementMessage streamData with
                    | Ok _ -> 
                        ()
                    | Error err ->
                        logger.LogError("Failed to send streaming announcement message. Received error: {0}", err)
                else
                    return ()
            | _, _ ->
                return ()
        }

    let handleSetStreamChannelCommand (msg : IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            if msg.Author.Id <> KnownUsers.DjurId then
                let djurMention = MentionUtils.MentionUser KnownUsers.DjurId

                sprintf "Only %s may set the stream channel at this time." djurMention
                |> MessageUtils.sendMessage msg.Channel 
            elif Seq.length msg.MentionedRoleIds <> 1 then
                MessageUtils.sendMessage msg.Channel "You must mention exactly 1 role to use as the streamer role."
            elif Seq.length msg.MentionedChannelIds <> 1 then
                MessageUtils.sendMessage msg.Channel "You must mention exactly 1 channel to use as the announcements channel."
            else
                let streamerRoleId = Seq.head msg.MentionedRoleIds
                let channelId = Seq.head msg.MentionedChannelIds
                let guildId = guildChannel.Guild.Id
                let channel = {
                    ChannelId = int64 channelId
                    GuildId = guildId
                    StreamerRoleId = int64 streamerRoleId
                }

                task {
                    do! database.SetStreamAnnouncementChannelForGuild channel

                    let returnMessage = 
                        sprintf "Stream announcement messages from streamers with the role "
                        + MentionUtils.MentionRole streamerRoleId
                        + " will be sent to the channel "
                        + MentionUtils.MentionChannel channelId
                        + "."

                    return! MessageUtils.sendMessage msg.Channel returnMessage
                }
        | :? ISocketPrivateChannel ->
            MessageUtils.sendMessage msg.Channel "Unable to set streams channel in a private message."
        | _ ->
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in unknown channel type."

    let handleUnsetStreamChannelCommand (msg : IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            if msg.Author.Id <> KnownUsers.DjurId then
                let djurMention = MentionUtils.MentionUser KnownUsers.DjurId

                sprintf "Only %s may unset the stream channel at this time." djurMention
                |> MessageUtils.sendMessage msg.Channel 
            else

                task {
                    do! database.DeleteStreamAnnouncementChannelForGuild guildId

                    return! MessageUtils.AddGreenCheckReaction msg
                }
        | :? ISocketPrivateChannel ->
            MessageUtils.sendMessage msg.Channel "Command is not supported in a private message."
        | _ ->
            MessageUtils.sendMessage msg.Channel "Command is not supported in unknown channel type."

    let commandReceived (msg : IMessage) = function
        | TestStreamStarted -> handleTestStreamStartedCommand msg
        | TestStreamEnded -> handleTestStreamEndedCommand msg
        | SetStreamsChannel -> handleSetStreamChannelCommand msg
        | UnsetStreamsChannel -> handleUnsetStreamChannelCommand msg
        | _ -> Task.empty
    
    interface IDisposable with
        member _.Dispose() =
            ()
            
    interface IHostedService with
        member _.StartAsync _ =
            bot.AddEventListener (DiscordEvent.UserUpdated userUpdated)
            bot.AddEventListener (DiscordEvent.CommandReceived commandReceived)
            Task.CompletedTask

        member _.StopAsync _ =
            Task.CompletedTask
