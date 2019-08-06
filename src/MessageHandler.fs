namespace Dijon 
open Discord
open System
open Discord.WebSocket

type MessageHandler(database: IDijonDatabase, client: DiscordSocketClient) = 
    let djurId = uint64 204665846386262016L
    let randomSlanderResponse () = 
        [
            "https://tenor.com/view/palpatine-treason-star-wars-emperor-gif-8547403"
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
            "https://tenor.com/view/futurama-bender-discrimination-funny-gif-5866137"
            "https://tenor.com/view/mass-effect-harbinger-collectors-video-games-reapers-gif-14520191"
            "https://tenor.com/view/youre-unbearably-naive-avengers-ultron-gif-10230820"
            "https://tenor.com/view/anakin-darth-vader-gif-5233555"
        ] 
        |> Seq.sortBy (fun _ -> Guid.NewGuid())
        |> Seq.head
    let embedField title value = EmbedFieldBuilder().WithName(title).WithValue(value)
    let sendMessage (channel: IMessageChannel) msg = channel.SendMessageAsync msg |> Async.AwaitTask |> Async.Ignore
    let sendEmbed (channel: IMessageChannel) (embed: EmbedBuilder) = channel.SendMessageAsync("", false, embed.Build()) |> Async.AwaitTask |> Async.Ignore
    let react (msg: SocketUserMessage) emote = msg.AddReactionAsync emote |> Async.AwaitTask |> Async.Ignore
    let mutliReact (msg: SocketUserMessage) (emotes: IEmote seq) = 
        emotes
        |> Seq.map (fun e -> fun _ -> react msg e)
        |> Async.Sequential

    let (|ContainsSlander|_|) (a: string) = 
        let tdeDownWithDjurChannel = 561289801890791425L
        if StringUtils.containsAny a ["#downwithdjur"; "down with djur"; ":downwithdjur:"; sprintf "<#%i>" tdeDownWithDjurChannel]
        then Some ContainsSlander 
        else None

    let (|Mentioned|NotMentioned|) (msg: IMessage) = 
        let mentionString = sprintf "<@%i> " client.CurrentUser.Id

        if StringUtils.startsWithAny msg.Content ["!dijon"; mentionString]
        then Mentioned
        else NotMentioned        

    let parseCommand (msg: IMessage): Command = 
        match msg with 
        | NotMentioned -> 
            match msg.Content with 
            | ContainsSlander -> Slander 
            | _ -> Ignore
        | Mentioned ->
            match StringUtils.stripFirstWord msg.Content |> StringUtils.lower |> StringUtils.trim with 
            | "goulash"
            | "goulash recipe"
            | "scrapple"
            | "scrapple recipe"
            | "recipe" -> Goulash
            | "test" -> Test
            | "status" -> Status
            | "set logs"
            | "log here"
            | "logs here"
            | "set logs here" 
            | "set log channel"
            | "set log channel here"
            | "set channel" -> SetLogChannel
            | "help"
            | "tutorial"
            | "commands"
            | "command" -> Help
            | "hype"
            | "hype me up"
            | "hype squad" 
            | "tell them how it is"
            | "tell em how it is"
            | "tell em"
            | "tell them" -> Hype
            | ContainsSlander -> Slander 
            | _ -> Unknown

    let handleTestMessage (msg: IMessage) (send: IMessageChannel -> GuildUser -> Async<unit>) =
        let fakeUser = {
            Nickname = Some "TestUser"
            UserName = "Discord"
            Discriminator = "0000"
            AvatarUrl = "https://placekitten.com/g/300/300"
        }

        send msg.Channel fakeUser

    let handleGoulashRecipe (msg: IMessage) = 
        let ingredients = [
            "- 10oz wide egg noodles"
            "- 1lb ground beef"
            "- 3/4 cup light brown sugar"
            "- 1 can (10.5oz) tomato soup"
            "- 1/8 cup ketchup"
        ]
        let instructions = [
            "1. Brown the ground beef and boil/drain the noodles."
            "2. Combine and mix t he beef, noodles, and the rest of the ingredients in one pot."
            "3. Let the mixture rest for at least 30 minutes, ideally 60 minutes."
            "4. Heat and serve with bread."
        ]
        let embed = EmbedBuilder()
        embed.Title <- "🤤 Sweet Goulash Recipe"
        embed.Color <- Nullable Color.Teal
        embed.Description <- "Here's the recipe for Djur's sweet goulash, the power food that fuels Team Tight Bois. **Highly** recommended by all those who've tried it, including Foxy, Jay and Patoosh."
        embed.ThumbnailUrl <- "https://az.nozzlegear.com/images/share/2019-07-30.16.13.11.png"
        embed.Fields.AddRange [
            embedField "Ingredients" (StringUtils.newlineJoin ingredients)
            embedField "Instructions" (StringUtils.newlineJoin instructions)
        ]

        sendEmbed msg.Channel embed 

    let handleStatusMessage (msg: IMessage) = 
        let embed = EmbedBuilder()
        embed.Title <- ":robot: Dijon Status"
        embed.Description <- sprintf ":heartbeat: **%i ms** heartbeat latency." client.Latency
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

                embed.Fields.AddRange [
                    embedField "Log Channel" logChannelMessage
                ]

                return! sendEmbed msg.Channel embed 
            }
        | _ -> 
            sendEmbed msg.Channel embed

    let handleSetLogChannelMessage (msg: IMessage) = 
        match msg.Channel with 
        | :? SocketGuildChannel as guildChannel -> 
            if msg.Author.Id <> djurId then
                sendMessage msg.Channel (sprintf "At the moment, only the Almighty <@%i> may set the log channel." djurId)
            else 
                async {
                    let guildId = GuildId (int64 guildChannel.Guild.Id)

                    do! database.SetLogChannelForGuild guildId (int64 msg.Channel.Id)
                    do! sendMessage msg.Channel "Messages will be sent to this channel when a user leaves the server."
                }
        | :? ISocketPrivateChannel -> 
            sendMessage msg.Channel "Unable to set log channel in a private message."
        | _ -> 
            sendMessage msg.Channel "Unable to set log channel in unknown channel type."

    let handleHelpMessage (msg: IMessage) =
        let embed = EmbedBuilder()
        embed.Title <- "⚡ Dijon-bot Commands" 
        embed.Color <- Nullable Color.Blue
        embed.Fields.AddRange [
            embedField "`status`" "Checks the status of Dijon-bot and reports which channel is used for logging membership changes."
            embedField "`set logs here`" "Tells Dijon-bot to report membership changes to the current channel. Only one channel is supported per server."
            embedField "`test`" "Sends a test membership change message to the current channel."
            embedField "`goulash recipe`" "Sends Djur's world-renowned sweet goulash recipe, the food that powers Team Tight Bois."
        ]

        sendEmbed msg.Channel embed

    let handleSlander (msg: IMessage) = 
        match msg with 
        | Mentioned -> 
            randomSlanderResponse ()
            |> sendMessage msg.Channel
        | NotMentioned ->   
            Async.Empty

    let handleHypeMessage (msg: IMessage) = 
        let msg = msg :?> SocketUserMessage
        if msg.Author.Id <> djurId then 
            ["🇳"; "🇴"]
            |> Seq.map Emoji
            |> Seq.cast<IEmote>
            |> mutliReact msg
        else
            let addReactions = fun _ -> 
                ["👌"; "🎉"; "🙇‍♀️"]
                |> Seq.map Emoji
                |> Seq.cast<IEmote>
                |> mutliReact msg
            [
                addReactions
                fun _ -> sendMessage msg.Channel "Here's a glimpse into Djur's average day: https://www.youtube.com/watch?v=hyNu5i_6lKA"
            ]
            |> Async.Sequential

    interface IMessageHandler with 
        member x.HandleMessage msg = 
            let self = x :> IMessageHandler

            match parseCommand msg with 
            | Test -> handleTestMessage msg self.SendUserLeftMessage 
            | Goulash -> handleGoulashRecipe msg 
            | Status -> handleStatusMessage msg 
            | SetLogChannel -> handleSetLogChannelMessage msg 
            | Slander -> handleSlander msg 
            | Help -> handleHelpMessage msg
            | Hype -> handleHypeMessage msg
            | Unknown
            | Ignore -> Async.Empty

        member x.SendUserLeftMessage channel user = 
            let nickname = Option.defaultValue user.UserName user.Nickname
            let message = sprintf "**%s** (%s#%s) has left the server." nickname user.UserName user.Discriminator
            let embed = EmbedBuilder()
            embed.Title <- "👋"
            embed.Description <- message
            embed.Color <- Nullable Color.DarkOrange
            embed.ThumbnailUrl <- user.AvatarUrl
            
            sendEmbed channel embed 
