namespace Dijon

open System
open Discord
open Discord.WebSocket
open System.Threading.Tasks
open FSharp.Control
open System.Threading
open System.Collections.Generic

module Bot = 
    let (+=) (evt: Func<'a, Task> -> unit) (handler: 'a -> Async<unit>) =
        let func = Func<'a, Task>(fun arg -> handler arg |> Async.StartAsTask :> Task)
        evt func 
    let (++=) (evt: Func<'a, 'b, Task> -> unit) (handler: 'a -> 'b -> Async<unit>) = 
        let func = Func<'a, 'b, Task>(fun a b -> handler a b |> Async.StartAsTask :> Task)
        evt func

    let private handleLogMessage (logMessage: LogMessage) = async { 
        let now = DateTimeOffset.UtcNow.ToString()
        printfn "[%s] %s: %s" now logMessage.Source logMessage.Message
    }

    let private listGuildUsers (guild: Rest.RestGuild) = guild.GetUsersAsync() |> Async.EnumerateCollection

    let private mapUsersToMembers (guild: Rest.RestGuild) =
        listGuildUsers guild 
        |> Async.SeqMap MemberUpdate.FromGuildUser 

    let private handleUserLeft (bot: BotConfig) (user: SocketGuildUser) = 
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
                |> bot.database.GetLogChannelForGuild 

            do! 
                match logChannelId with 
                | Some channelId -> 
                    let channel = user.Guild.GetTextChannel (uint64 channelId)
                    bot.messages.SendUserLeftMessage channel userData
                | None -> Async.Empty
            
            // Delete the user so the app doesn't find it at next startup
            do! bot.database.DeleteAsync (UniqueUser.FromSocketGuildUser user)
        }

    let private handleUserJoined (bot: BotConfig) (user: SocketGuildUser) = 
        bot.database.BatchSetAsync [MemberUpdate.FromGuildUser user]

    let private handleUserUpdated (bot: BotConfig) (before: SocketGuildUser) (after: SocketGuildUser) =
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
                    
                printfn "Adding %s role for user %s#%s" memberRole.Name nickname after.Discriminator
                
                do! after.AddRoleAsync memberRole
                    |> Async.AwaitTask
            }
        | _, _ ->
            Async.Empty
            // bot.database.BatchSetAsync [MemberUpdate.FromGuildUser after]
        
    
    let handleLeftGuild (bot: BotConfig) (guild: SocketGuild) = 
        bot.database.UnsetLogChannelForGuild (GuildId <| int64 guild.Id)

    let Connect botToken = 
        let client = new DiscordSocketClient()

        client.add_Log += handleLogMessage

        async {
            do! client.LoginAsync(TokenType.Bot, botToken) |> Async.AwaitTask
            do! client.StartAsync() |> Async.AwaitTask
            do! client.SetGameAsync "This Is Legal But We Question The Ethics" |> Async.AwaitTask

            return client 
        }
    
    let RecordAllUsers (bot: BotConfig) = async {
        let! allGuilds = 
            bot.client.Rest.GetGuildsAsync() 
            |> Async.AwaitTask
        let! guildMembers = 
            allGuilds 
            |> Seq.map mapUsersToMembers
            |> Async.Parallel
            |> Async.Map Seq.concat

        do! bot.database.BatchSetAsync guildMembers
        // TODO: the bot should look for members that may have left while it was offline and announce them to the configured channel
    }

    let WireEventListeners onReady (bot: BotConfig) =
        let client = bot.client

        client.add_MessageReceived += bot.messages.HandleMessage 
        client.add_UserLeft += handleUserLeft bot
        client.add_UserJoined += handleUserJoined bot
        client.add_LeftGuild += handleLeftGuild bot
        client.add_GuildMemberUpdated ++= handleUserUpdated bot
        
        let func = Func<Task>(fun _ -> onReady (); Task.CompletedTask)
        client.add_Ready func 

        Async.Empty