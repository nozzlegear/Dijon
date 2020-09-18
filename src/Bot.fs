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

    let private handleGuildUserUpdated (bot: BotConfig) (before: SocketGuildUser) (after: SocketGuildUser) =
        
        printfn "Guild user %s has been updated. Are they streaming? %b" after.Nickname after.IsStreaming
        printfn "Guild user %s activity: %s: %s (type: %A)" after.Nickname after.Activity.Name after.Activity.Details after.Activity.Type
        
        match after.Activity with
        | :? CustomStatusGame as custom ->
            printfn "user has a custom status -- Name: %s; Details: %s; State: %s" custom.Name custom.Details custom.State
        | :? SpotifyGame as spotify ->
            printfn "user %s is listening to %s on spotify" after.Nickname spotify.AlbumTitle
        | :? StreamingGame as stream ->
            printfn "user %s is streaming %s at URL %s" after.Nickname stream.Name stream.Url
        | x ->
            printfn "User has an activity that is not a custom status: %A" x 
        
        // TODO: detect if a user is streaming, and if so, detect _what_ they're streaming
        match before.IsStreaming, after.IsStreaming with
        | true, false ->
            // User has stopped streaming
            // TODO: delete the stream message from the streaming channel
            
            printfn "User %s has stopped streaming" after.Nickname
            
            ()
        | false, true ->
            // User has started streaming
            // TODO: add an "is streaming" message in the streaming channel
            let userStatus = after.Activity.Details
            
            printfn "User %s has started streaming" after.Nickname
            
            ()
        | _, _ -> 
            // Nothing has changed 
            ()
        
        // Temporarily disable the update handler, which is blocking and causing issues
        Async.Empty
        // bot.database.BatchSetAsync [MemberUpdate.FromGuildUser after]
        
    let private handleUserUpdated (bot: BotConfig) (before: SocketUser) (after: IUser) =
        printfn "User %s has been updated" after.Username
        Async.Empty
    
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

    let WireEventListeners (bot: BotConfig) =
        let client = bot.client

        client.add_MessageReceived += bot.messages.HandleMessage 
        client.add_UserLeft += handleUserLeft bot
        client.add_UserJoined += handleUserJoined bot
        client.add_LeftGuild += handleLeftGuild bot
        client.add_GuildMemberUpdated ++= handleGuildUserUpdated bot
        client.add_UserUpdated ++= handleUserUpdated bot 

        Async.Empty