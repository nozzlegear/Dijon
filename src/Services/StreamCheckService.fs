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

    let sendStreamAnnouncementMessage (channel : IChannel) (user  : IUser) (stream : StreamingGame) =
        async {
            let embed = EmbedBuilder()
            let username = 
                match user with
                | :? IGuildUser as guildUser when not (String.IsNullOrEmpty guildUser.Nickname) -> guildUser.Nickname
                | _ -> user.Username
            embed.Title <- sprintf "%s is live on %s right now!" username stream.Name
            embed.Color <- Nullable Color.Green
            embed.Description <- sprintf "**%s**" stream.Details
            embed.Url <- stream.Url

            return! MessageUtils.sendEmbed (channel :?> IMessageChannel) embed
        }

    let postStreamingMessage (user : IGuildUser) (stream : StreamingGame) : Async<unit> =
        async {
            let guildId = GuildId <| int64 user.Guild.Id
            
            match! database.GetStreamAnnouncementChannelForGuild guildId with
            | Some channelData ->
                let channel = bot.GetChannel channelData.ChannelId
                let! messageId = sendStreamAnnouncementMessage channel user stream
                let announcementMessage =
                    { ChannelId = channelData.ChannelId
                      MessageId = messageId
                      GuildId = int64 user.Guild.Id
                      StreamerId = int64 user.Id }

                do! database.AddStreamAnnouncementMessage announcementMessage
            | None ->
                ()
        }

    let removeStreamingMessage (user : IGuildUser) : Async<unit> = 
        logger.LogInformation("Removing streaming message for user {0}", user.Id)
        Async.Empty

    let handleTestStreamStartedCommand (msg : IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            async {
                let guildId = int64 guildChannel.Guild.Id |> GuildId

                match! database.GetStreamAnnouncementChannelForGuild guildId with
                | Some channelData ->
                    let channel = bot.GetChannel channelData.ChannelId
                    let user = msg.Author
                    let stream = StreamingGame("Twitch", "https://twitch.tv/nozzlegear")
                    // stream.Details <- "This is the title of the test stream. It's an amazing and fun stream!"
                    let! messageId = sendStreamAnnouncementMessage channel user stream
                    let announcementMessage =
                        { ChannelId = channelData.ChannelId
                          GuildId = int64 guildChannel.Guild.Id
                          MessageId = messageId
                          StreamerId = int64 <| bot.GetBotUserId() }

                    do! database.AddStreamAnnouncementMessage announcementMessage
                    
                    return! MessageUtils.react (msg :?> SocketUserMessage) (Emoji "✅")
                | None ->
                    return! MessageUtils.sendMessage msg.Channel "This guild does not have a stream announcements channel set."
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
                    let! members = 
                        guild.GetUsersAsync() 
                        |> Async.AwaitTask

                    // Check each member to see if they're streaming
                    for guildMember in members do
                        match tryGetStreamActivity guildMember with
                        | Some activity ->
                            // User is streaming, announce it to the stream channel
                            do! postStreamingMessage guildMember activity
                        | None ->
                            let totalActivities = Seq.length guildMember.Activities
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
            postStreamingMessage after stream
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
