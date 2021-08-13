namespace Dijon.Services

open Dijon
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive
open Discord
open Discord.WebSocket

type StreamCheckService(logger : ILogger<StreamCheckService>, 
                        database : IDijonDatabase, 
                        bot : Dijon.BotClient) =
            
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

    let buildStreamAnnouncementEmbed (stream : StreamData) =
        // TODO: use the Twitch API to get a stream preview image
        let embed = EmbedBuilder()
        let nickname = MessageUtils.GetNickname stream.User
        embed.Title <- sprintf "%s is live on %s right now!" nickname stream.Name
        embed.Color <- Nullable Color.Green
        embed.Description <- sprintf "**%s**" stream.Details
        embed.Url <- stream.Url
        embed.ThumbnailUrl <- stream.User.GetAvatarUrl(size = uint16 256)
        embed

    let sendStreamAnnouncementMessage (stream : StreamData) =
        async {
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

    let removeStreamingMessage (user : IGuildUser) : Async<unit> = 
        logger.LogInformation("Removing streaming message for user {0}", user.Id)
        Async.Empty

    let handleTestStreamStartedCommand (msg : IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            async {
                let streamData =
                    { Name = "Twitch"
                      Details = "An amazing test stream that's not actually live right now! It's a test!"
                      Url = "https://twitch.tv/nozzlegear"
                      User = msg.Author
                      GuildId = int64 guildChannel.Guild.Id }

                match! sendStreamAnnouncementMessage streamData with
                | Ok _ ->
                    return! MessageUtils.react (msg :?> SocketUserMessage) (Emoji "✅")
                | Error err ->
                    return! sprintf "Error: %s" err
                            |> MessageUtils.sendMessage msg.Channel
            }
        | :? ISocketPrivateChannel ->
            MessageUtils.sendMessage msg.Channel "Command is not supported in a private message."
        | _ ->
            MessageUtils.sendMessage msg.Channel "Command is not supported in unknown channel type."

    let handleTestStreamEndedCommand (msg : IMessage) =
        Async.Empty

    let scanActiveStreamers () : Async<unit> = 
        logger.LogInformation("Scanning for active streamers.")
        
        let rec scanNextGuild (remaining : IGuild list) =
            match remaining with
            | [] -> 
                Async.Empty
            | guild :: remaining ->
                async {
                    // Make sure this guild has a stream channel
                    let guildId = int64 guild.Id

                    match! database.GetStreamAnnouncementChannelForGuild (GuildId guildId) with
                    | Some _ ->
                        let! members = 
                            guild.GetUsersAsync() 
                            |> Async.AwaitTask

                        // Check each member to see if they're streaming
                        for guildMember in members do
                            match tryGetStreamActivity guildMember with
                            | Some activity ->
                                // User is streaming, announce it to the stream channel
                                let streamData = 
                                    { User = guildMember
                                      Name = activity.Name
                                      Details = activity.Details
                                      Url = activity.Url
                                      GuildId = int64 guild.Id }
                                do! sendStreamAnnouncementMessage streamData
                                    |> Async.Ignore
                            | None ->
                                ()
                    | None ->
                        ()

                    return! scanNextGuild remaining
                }

        async {
            // Update the list of guild members
            let! allGuilds = 
                bot.ListGuildsAsync() 
                |> Async.AwaitTask

            do! allGuilds
                |> List.ofSeq
                |> scanNextGuild
        }

    let userUpdated (before : SocketGuildUser) (after : SocketGuildUser) = 
        let streamerRoleId = 
            uint64 856350523812610048L
        let hasStreamerRole = 
            after.Roles
            |> Seq.exists (fun r -> r.Id = streamerRoleId)
        let wasStreaming = isStreaming before
        let stream = tryGetStreamActivity after

        match wasStreaming, stream with
        | true, None ->
            removeStreamingMessage after
        | false, Some stream when hasStreamerRole ->
            // User is streaming, announce it to the stream channel
            let streamData = 
                { User = after
                  Name = stream.Name
                  Details = stream.Details
                  Url = stream.Url
                  GuildId = int64 after.Guild.Id }
            sendStreamAnnouncementMessage streamData
            |> Async.Ignore
        | _, _ ->
            Async.Empty

    let handleSetStreamChannelCommand (msg : IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            if Seq.length msg.MentionedRoleIds <> 1 then
                MessageUtils.sendMessage msg.Channel "You must mention exactly 1 role to use as the streamer role."
            elif Seq.length msg.MentionedChannelIds <> 1 then
                MessageUtils.sendMessage msg.Channel "You must mention exactly 1 channel to use as the announcements channel."
            else
                let streamerRoleId = Seq.head msg.MentionedRoleIds
                let channelId = Seq.head msg.MentionedChannelIds
                let guildId = guildChannel.Guild.Id

                async {
                    let channel = 
                        { ChannelId = int64 channelId
                          GuildId = int64 guildId
                          StreamerRoleId = int64 streamerRoleId }

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
            async {
                let guildId = int64 guildChannel.Guild.Id |> GuildId

                do! database.DeleteStreamAnnouncementChannelForGuild guildId

                return! MessageUtils.react (msg :?> SocketUserMessage) (Emoji "✅")
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
        | _ -> Async.Empty
    
    interface IDisposable with
        member _.Dispose() =
            ()
            
    interface IHostedService with
        member _.StartAsync cancellation =
            task {
                do! bot.AddEventListener (DiscordEvent.UserUpdated userUpdated)
                do! bot.AddEventListener (DiscordEvent.CommandReceived commandReceived)
                do! scanActiveStreamers ()
            }
            :> Task
            
        member _.StopAsync _ =
            Task.CompletedTask
