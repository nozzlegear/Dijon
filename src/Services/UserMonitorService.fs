namespace Dijon.Services

open Dijon
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Discord
open Discord.WebSocket
open FSharp.Control.Tasks.V2.ContextInsensitive

type UserMonitorService(logger : ILogger<UserMonitorService>, bot : Dijon.BotClient, database : IDijonDatabase, messages : IMessageHandler) =
    let mapUsersToMembers (guild: IGuild) =
        async {
            let! users = 
                guild.GetUsersAsync()
                |> Async.AwaitTask

            return users
                   |> Seq.map MemberUpdate.FromGuildUser
        }

    let userJoined (user : SocketGuildUser) = 
        database.BatchSetAsync [MemberUpdate.FromGuildUser user]

    let userLeft (user : SocketGuildUser) = 
        let userData: GuildUser = {
            AvatarUrl = user.GetAvatarUrl(size = uint16 1024)
            UserName = user.Username
            Discriminator = user.Discriminator
            Nickname = Option.ofObj user.Nickname
        }    

        async {
            let! logChannelId = 
                int64 user.Guild.Id
                |> GuildId
                |> database.GetLogChannelForGuild 

            do! 
                match logChannelId with 
                | Some channelId -> 
                    let channel = user.Guild.GetTextChannel (uint64 channelId)
                    messages.SendUserLeftMessage channel userData
                | None -> 
                    Async.Empty
            
            // Delete the user so the app doesn't find it at next startup
            do! database.DeleteAsync (UniqueUser.FromSocketGuildUser user)
        }

    let botLeftGuild (guild : SocketGuild) = 
        int64 guild.Id
        |> GuildId
        |> database.UnsetLogChannelForGuild 

    let userUpdated (before : SocketGuildUser) (after : SocketGuildUser) =
        // Check if this user was just assigned a raider/team role
        let memberRoleId =
            uint64 427327334119637014L
        let raiderRoleIds =
            [ uint64 424683167899713537L (* Raiders *)
              uint64 460303590909673472L (* Last Call *)
              uint64 460303540859043842L (* Team Tight Bois *)
              uint64 534792074780999690L (* Weekend Raid *) ]
        let hasNewRaiderRole =
            after.Roles
            |> Seq.except before.Roles
            |> Seq.fold (fun state role -> if Seq.contains role.Id raiderRoleIds then true else state ) false
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
                    
            async {
                let memberRole =
                    after.Guild.Roles
                    |> Seq.find (fun r -> r.Id = memberRoleId)
                    
                logger.LogInformation("Adding %s role for user %s#%s", memberRole.Name, nickname, after.Discriminator)
                
                do! after.AddRoleAsync memberRole
                    |> Async.AwaitTask
            }
        | _, _ ->
            Async.Empty
            // bot.database.BatchSetAsync [MemberUpdate.FromGuildUser after]


    interface IDisposable with
        member _.Dispose() =
            ()

    interface IHostedService with
        member _.StartAsync _ = 
            task {
                // Wire event listeners
                do! bot.AddEventListener (DiscordEvent.UserJoined userJoined)
                do! bot.AddEventListener (DiscordEvent.UserLeft userLeft)
                do! bot.AddEventListener (DiscordEvent.UserUpdated userUpdated)
                do! bot.AddEventListener (DiscordEvent.BotLeftGuild botLeftGuild)

                // Update the list of guild members
                let! allGuilds = 
                    bot.ListGuildsAsync() 
                    |> Async.AwaitTask
                let! guildMembers = 
                    allGuilds 
                    |> Seq.map mapUsersToMembers
                    |> Async.Parallel
                    |> Async.Map Seq.concat

                do! database.BatchSetAsync guildMembers

                logger.LogInformation "Updated list of current guild members"

                // TODO: the bot should look for members that may have left while it was offline and announce them to the configured channel

            } 
            :> Task
            
        member _.StopAsync _ =
            Task.CompletedTask
