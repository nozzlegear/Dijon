namespace Dijon.Services

open Dijon
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive
open Discord
open Discord.WebSocket

type StreamCheckService(logger : ILogger<StreamCheckService>, bot : Dijon.BotClient) =
            
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

    let postStreamingMessage (user : IGuildUser) (stream : StreamingGame) : Async<unit> =
        let embed = EmbedBuilder()
        embed.Title <- sprintf "%s is streaming on %s right now!" user.Nickname stream.Name
        embed.Color <- Nullable Color.Green
        embed.Description <- sprintf "**%s**" stream.Details
        embed.Url <- stream.Url

        // TODO: get the server's stream messages channel id
        let channelId = int64 856354026509434890L
        let channel = bot.GetChannel channelId

        MessageUtils.sendEmbed (channel :> IChannel :?> IMessageChannel) embed

    let removeStreamingMessage (user : IGuildUser) : Async<unit> = 
        logger.LogInformation("Removing streaming message for user {0}", user.Id)
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
    
    interface IDisposable with
        member _.Dispose() =
            ()
            
    interface IHostedService with
        member _.StartAsync cancellation =
            task {
                do! bot.AddEventListener (DiscordEvent.UserUpdated userUpdated)
                do! scanActiveStreamers ()
            }
            :> Task
            
        member _.StopAsync _ =
            Task.CompletedTask
