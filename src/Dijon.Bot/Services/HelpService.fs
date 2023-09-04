namespace Dijon.Services

open Dijon
open Discord
open Discord.WebSocket
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive

type HelpService(logger : ILogger<HelpService>, 
                 bot : Dijon.BotClient, 
                 database : IDijonDatabase) =

    let handleStatusCommand (msg: IMessage) =
        let embed = EmbedBuilder()
        embed.Title <- ":robot: Dijon Status"
        embed.Description <- sprintf ":heartbeat: **%i ms** heartbeat latency." (bot.GetLatency ())
        embed.Color <- Nullable Color.Green

        // If this message was sent in a guild channel, report which channel it logs to
        match msg.Channel with 
        | :? SocketGuildChannel as guildChannel ->
            let guildId = GuildId (int64 guildChannel.Guild.Id)

            async {
                let! logChannelId = database.GetLogChannelForGuild guildId
                let logChannelMessage = 
                    logChannelId
                    |> Option.map (sprintf "Membership logs for this server are sent to the <#%i> channel.")
                    |> Option.defaultValue "Member logs are **not set up** for this server. Use `!dijon log here` to set the log channel."

                let! affixChannel = database.GetAffixChannelForGuild guildId
                let affixChannelMessage =
                    affixChannel
                    |> Option.map (fun channel -> sprintf "Mythic Plus affixes messages for this server are sent to the <#%i> channel." channel.ChannelId)
                    |> Option.defaultValue "Mythic Plus affixes are **not set up** for this server. Use `!dijon set affixes here` to set the affix channel."

                let! streamChannel = database.GetStreamAnnouncementChannelForGuild guildId
                let streamChannelMessage =
                    streamChannel
                    |> Option.map (fun channel -> sprintf "Stream announcement messages for this server are sent to the <#%i> channel when a user with the role %s role goes live." channel.ChannelId (MentionUtils.MentionRole <| uint64 channel.StreamerRoleId))
                    |> Option.defaultValue "Stream announcement messages are **not set up** for this server. Use `!dijon set streams in #channel for @streamerRole` to set the stream announcements chanenl."

                embed.Fields.AddRange [
                    MessageUtils.embedField "Log Channel" logChannelMessage
                    MessageUtils.embedField "Affixes Channel" affixChannelMessage
                    MessageUtils.embedField "Stream Announcements Channel" streamChannelMessage
                ]

                return! MessageUtils.sendEmbed msg.Channel embed 
                        |> Async.Ignore
            }
        | _ -> 
            MessageUtils.sendEmbed msg.Channel embed
            |> Async.Ignore

    let handleHelpCommand (msg: IMessage) =
        let embed = EmbedBuilder()
        embed.Title <- "⚡ Dijon-bot Commands" 
        embed.Color <- Nullable Color.Blue
        embed.Fields.AddRange [
            MessageUtils.embedField "`affixes`" "Fetches this week's Mythic+ dungeon affixes and displays them alongside a description of each."
            MessageUtils.embedField "`status`" "Checks the status of Dijon-bot and reports which channel is used for logging membership changes."
            MessageUtils.embedField "`set logs here`" "Tells Dijon-bot to report membership changes to the current channel. Only one channel is supported per server."
            MessageUtils.embedField "`set affixes here`" "Tells Dijon-bot to post Mythic Plus affixes to the current channel every Tuesday. Only one channel is supported per server."
            MessageUtils.embedField "`set streams #announcementChannel @streamerRole`" "Tells Dijon-bot to announce the streams of any member with the @streamerRole role to the given channel. Only one channel and one streamer role is supported per server."
            MessageUtils.embedField "`test`" "Sends a test membership change message to the current channel."
            MessageUtils.embedField "`goulash recipe`" "Sends Djur's world-renowned sweet goulash recipe, the food that powers Team Tight Bois."
        ]

        MessageUtils.sendEmbed msg.Channel embed
        |> Async.Ignore

    let handleUnknownCommand (msg: IMessage) = 
        MessageUtils.AddShrugReaction msg

    let handleCommand (msg : IMessage) = function
        | Help -> handleHelpCommand msg
        | Status -> handleStatusCommand msg
        | Unknown -> handleUnknownCommand msg
        | _ -> Async.Empty

    interface IDisposable with
        member _.Dispose() = 
            ()

    interface IHostedService with
        member _.StartAsync cancellation =
            task {
                do! bot.AddEventListener (DiscordEvent.CommandReceived handleCommand)
            }
            :> Task

        member _.StopAsync _ =
            Task.CompletedTask
