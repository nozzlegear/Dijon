namespace Dijon 
open Discord
open System
open Discord.WebSocket

type MessageHandler(database: IDijonDatabase, client: DiscordSocketClient) = 
    let randomSlanderResponse () = 
        [
            "https://tenor.com/view/palpatine-treason-star-wars-emperor-gif-8547403"
            "https://tenor.com/view/war-badass-fight-arnold-schwarzenegger-schwarzenegger-gif-5373801"
            "https://tenor.com/view/thanos-infinity-war-avengers-gif-10387727" 
            "https://tenor.com/view/t800-terminator-robot-war-gif-14523522"
            "https://tenor.com/view/talk-to-the-hand-arnold-schwarzenegger-terminator-quote-movie-gif-13612079"
            "https://tenor.com/view/battlestar-galactica-battlestar-galactica-katee-sackhoff-mar-mc-donnell-gif-4414461"
            "https://tenor.com/view/the-matrix-the-architect-there-you-go-voila-gif-7941059"
            "https://tenor.com/view/white-guy-blink-baelish-little-finger-what-confused-gif-9608513"
            "https://tenor.com/view/palpatine-starwars-arrogance-your-arrogance-blinds-you-gif-7521418"
            "https://tenor.com/view/palpatine-starwars-lightning-emperor-gif-6199498"
            "https://tenor.com/view/sheev-palpatine-emperor-chancellor-kill-him-now-gif-14424446"
            "https://tenor.com/view/anakin-glare-killing-young-jedi-gif-5770427"
            "https://tenor.com/view/legion-geth-mass-effect-shock-amazed-gif-8578526"
        ] 
        |> Seq.sortBy (fun _ -> Guid.NewGuid())
        |> Seq.head

    let (|ContainsSlander|_|) (a: string) = 
        if StringUtils.containsAny a ["#downwithdjur"; "down with djur"; ":downwithdjur:"]
        then Some ContainsSlander 
        else None

    let (|Mentioned|NotMentioned|) (msg: IMessage) = 
        let mentionString = sprintf "<@%i> " client.CurrentUser.Id

        if StringUtils.startsWithAny msg.Content ["!dijon"; mentionString]
        then Mentioned
        else NotMentioned        

    let (|Ignore|Test|Goulash|Status|SetLogChannel|Slander|BadCommand|) (msg: IMessage) = 
        match msg with 
        | NotMentioned -> 
            match msg.Content with 
            | ContainsSlander -> Slander 
            | _ -> Ignore
        | Mentioned ->
            match StringUtils.stripFirstWord msg.Content |> StringUtils.lower |> StringUtils.trim with 
            | "goulash"
            | "goulash recipe"
            | "recipe" -> Goulash
            | "test" -> Test
            | "status" -> Status
            | "set log channel" -> SetLogChannel
            | ContainsSlander -> Slander 
            | _ -> BadCommand

    let handleTestMessage  (msg: IMessage) =
        let weekAgo = DateTimeOffset.Now.AddDays -7.
        let embed = EmbedBuilder()
        embed.Title <- "A user has left the server."
        embed.Description <- sprintf "User SomeNickname (Discord#0123) has left the server. They were first seen at %O." weekAgo
        embed.Color <- Nullable Color.DarkOrange

        msg.Channel.SendMessageAsync("", false, embed.Build())
        |> Async.AwaitTask
        |> Async.Ignore

    let handleGoulashRecipe (msg: IMessage) = 
        let embed = EmbedBuilder()
        embed.Title <- "🤤 Sweet Goulash Recipe 🤤"
        embed.Description <- "Here's the recipe for Djur's sweet goulash, the power food that fuels Team Tight Bois. **Highly** recommended by all those who've tried it, including Foxy, Jay and Patoosh."
        embed.ImageUrl <- "https://cdn.discordapp.com/attachments/544538425437716491/544540300958236676/Screenshot_20190208-221800.png"
        embed.Color <- Nullable Color.Teal

        msg.Channel.SendMessageAsync("", false, embed.Build())
        |> Async.AwaitTask
        |> Async.Ignore

    let handleStatusMessage (msg: IMessage) = 
        let embed = EmbedBuilder()
        embed.Title <- ":robot: Dijon Status"
        embed.Description <- sprintf ":heartbeat: **%i ms** heartbeat latency." client.Latency
        embed.Color <- Nullable Color.Green

        msg.Channel.SendMessageAsync("", false, embed.Build())
        |> Async.AwaitTask
        |> Async.Ignore

    let handleSetLogChannelMessage (msg: IMessage) = async {
        printfn "Handling set log channel message"
    }

    let handleBadCommandMessage (msg: IMessage) = Async.Empty

    let handleSlander (msg: IMessage) = 
        match msg with 
        | Mentioned -> 
            let randomMsg = randomSlanderResponse () 

            msg.Channel.SendMessageAsync randomMsg
            |> Async.AwaitTask 
            |> Async.Ignore
        | NotMentioned -> 
            let msg = msg :?> SocketUserMessage

            msg.AddReactionAsync (Emoji "💔")
            |> Async.AwaitTask
            |> Async.Ignore

    interface IMessageHandler with 
        member x.HandleMessage msg = 
            match msg with 
            | Ignore -> Async.Empty
            | Test -> handleTestMessage msg 
            | Goulash -> handleGoulashRecipe msg 
            | Status -> handleStatusMessage msg 
            | SetLogChannel -> handleSetLogChannelMessage msg 
            | BadCommand -> handleBadCommandMessage msg
            | Slander -> handleSlander msg 
