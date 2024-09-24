namespace Dijon.Bot.Services

open Dijon.Bot
open Dijon.Database
open Dijon.Shared
open Dijon.Database.LogChannels

open Discord
open Discord.WebSocket
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System
open System.Threading.Tasks

type GuildUser =
    {
        Nickname: string option
        UserName: string
        Discriminator: string
        AvatarUrl: string
    }

type UserMonitorService(
    logger : ILogger<UserMonitorService>,
    bot : IBotClient,
    logChannelDatabase: ILogChannelsDatabase
) =
    let sendUserLeftMessage channel (user: GuildUser) =
        let nickname = Option.defaultValue user.UserName user.Nickname
        let message = $"**%s{nickname}** (%s{user.UserName}#%s{user.Discriminator}) has left the server."
        let embed = EmbedBuilder()
        embed.Title <- "👋"
        embed.Description <- message
        embed.Color <- Nullable Color.DarkOrange
        embed.ThumbnailUrl <- user.AvatarUrl
        
        MessageUtils.sendEmbed channel embed
        |> Task.ignore

    let userLeft (guild: SocketGuild) (user: SocketUser) = 
        let userData: GuildUser = {
            AvatarUrl = user.GetAvatarUrl(size = uint16 1024)
            UserName = user.Username
            Discriminator = user.Discriminator
            Nickname = match user with
                       | :? SocketGuildUser as g -> Some g.Nickname
                       | _ -> None
        }

        task {
            let! logChannelId = 
                int64 guild.Id
                |> GuildId
                |> logChannelDatabase.GetLogChannelForGuild

            do! 
                match logChannelId with 
                | Some channelId -> 
                    let channel = guild.GetTextChannel (uint64 channelId)
                    sendUserLeftMessage channel userData
                | None -> 
                    Task.empty
        }

    let botLeftGuild (guild : SocketGuild) = 
        int64 guild.Id
        |> GuildId
        |> logChannelDatabase.UnsetLogChannelForGuild

    let userUpdated (before: CachedGuildUser) (after: SocketGuildUser) =
        // Check if this user was just assigned a raider/team role
        let memberRoleId =
            uint64 427327334119637014L
        let raiderRoleIds =
            [ uint64 424683167899713537L (* Raiders *)
              uint64 460303590909673472L (* Last Call *)
              uint64 460303540859043842L (* Team Tight Bois *)
              uint64 534792074780999690L (* Weekend Raid *) ]
        task {
            let! before = before.GetOrDownloadAsync()
            let hasNewRaiderRole =
                after.Roles
                |> Seq.except before.Roles
                |> Seq.fold (fun state role -> 
                    if Seq.contains role.Id raiderRoleIds 
                    then true 
                    else state
                    ) false
            let hasMemberRole =
                after.Roles
                |> Seq.exists (fun r -> r.Id = memberRoleId)

            match hasNewRaiderRole, hasMemberRole with
            | true, false ->
                // Assign the member role to the user
                let nickname =
                    if String.IsNullOrWhiteSpace after.Nickname then
                        after.Username
                    else
                        after.Nickname
                        
                let memberRole =
                    after.Guild.Roles
                    |> Seq.find (fun r -> r.Id = memberRoleId)
                    
                logger.LogInformation("Adding {RoleName} role for user {UserName}", memberRole.Name, $"{nickname}#{after.Discriminator}")
                
                do! after.AddRoleAsync memberRole
            | _, _ ->
                ()
        }

    let handleSetLogChannelCommand (msg: IMessage) = 
        match msg.Channel with 
        | :? SocketGuildChannel as guildChannel -> 
            if msg.Author.Id <> KnownUsers.DjurId then
                "At the moment, only the Almighty "
                + MentionUtils.MentionUser KnownUsers.DjurId
                + " may set the log channel."
                |> MessageUtils.sendMessage msg.Channel
            else 
                task {
                    let guildId = GuildId (int64 guildChannel.Guild.Id)

                    do! logChannelDatabase.SetLogChannelForGuild guildId (int64 msg.Channel.Id)
                    do! MessageUtils.sendMessage msg.Channel "Messages will be sent to this channel when a user leaves the server."
                }
        | :? ISocketPrivateChannel -> 
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in a private message."
        | _ -> 
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in unknown channel type."

    /// Sends a test message indicating that a fake user left the server.
    let handleTestUserLeftCommand (msg : IMessage) = 
        let fakeUser: GuildUser = {
            Nickname = Some "TestUser"
            UserName = "Discord"
            Discriminator = "0000"
            AvatarUrl = msg.Author.GetAvatarUrl(size = uint16 1024)
        }

        sendUserLeftMessage msg.Channel fakeUser

    let handleCommand (msg : IMessage) = function
        | SetLogChannel -> handleSetLogChannelCommand msg
        | TestUserLeft -> handleTestUserLeftCommand msg
        | _ -> Task.empty

    interface IDisposable with
        member _.Dispose() =
            ()

    interface IHostedService with
        member _.StartAsync _ = 
            task {
                // Wire event listeners
                bot.AddEventListener (DiscordEvent.UserLeft userLeft)
                bot.AddEventListener (DiscordEvent.UserUpdated userUpdated)
                bot.AddEventListener (DiscordEvent.BotLeftGuild botLeftGuild)
                bot.AddEventListener (DiscordEvent.CommandReceived handleCommand)

                // TODO: the bot should look for members that may have left while it was offline and announce them to the configured channel
            }

        member _.StopAsync _ =
            Task.CompletedTask
