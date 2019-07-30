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

    let private logEvent = Func<LogMessage, Task>(fun evt -> 
        printfn "%s: %s" evt.Source evt.Message

        Task.CompletedTask
    )
    
    let private enumerate (collection: IAsyncEnumerable<_ IReadOnlyCollection>) = 
        let cancellationToken = CancellationToken()
        let rec iterate (enumerator: IAsyncEnumerator<_ IReadOnlyCollection>) (gathered: _ seq) = async {
            let! shouldContinue = enumerator.MoveNext cancellationToken |> Async.AwaitTask

            if not shouldContinue then 
                return gathered
            else
                return! iterate enumerator (Seq.concat [gathered; seq enumerator.Current])
        }
        iterate (collection.GetEnumerator()) []

    let private listGuildUsers (guild: Rest.RestGuild) = guild.GetUsersAsync() |> enumerate

    let private mapUsersToMembers (guild: Rest.RestGuild) =
        listGuildUsers guild 
        |> Async.SeqMap MemberUpdate.FromGuildUser 

    let private handleUserLeft (user: SocketGuildUser) = 
        Async.Empty

    let private handleUserJoined (user: SocketGuildUser) = 
        Async.Empty

    let private handleUserUpdated (before: SocketGuildUser) (after: SocketGuildUser) = 
        if before.Nickname = after.Nickname && before.Username = after.Username && before.Discriminator = after.Discriminator then
            Async.Empty 
        else
            printfn "Guild member %s#%s with nickname %s is now %s" before.Username before.Discriminator before.Nickname after.Nickname
            Async.Empty

    let Connect botToken = 
        let client = new DiscordSocketClient()

        client.add_Log logEvent

        async {
            do! client.LoginAsync(TokenType.Bot, botToken) |> Async.AwaitTask
            do! client.StartAsync() |> Async.AwaitTask

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
        client.add_UserLeft += handleUserLeft
        client.add_UserJoined += handleUserJoined
        client.add_GuildMemberUpdated ++= handleUserUpdated

        Async.Empty