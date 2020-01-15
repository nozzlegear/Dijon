namespace Dijon 
open Discord
open System
open Discord.WebSocket

type MessageHandler(database: IDijonDatabase, client: DiscordSocketClient) = 
    let djurId = uint64 204665846386262016L
    let foxyId = uint64 397255457862975509L
    let calyId = uint64 148990194815598592L
    let biggelsId = uint64 479036597312946177L
    let randomSlanderResponse () = 
        [ "https://tenor.com/view/palpatine-treason-star-wars-emperor-gif-8547403"
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
          "https://tenor.com/view/anakin-darth-vader-gif-5233555" ]
        |> Seq.randomItem
    let formatMentionString = sprintf "<@%i> "
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
        let slanderMessages = 
            [ "#downwithdjur" 
              "down with djur"
              ":downwithdjur:"
              sprintf "<#%i>" tdeDownWithDjurChannel ]

        if StringUtils.containsAny a slanderMessages then
            Some ContainsSlander 
        else
            None

    let (|AsksWhereFoxyIs|_|) (a: string) = 
        let whereIsFoxyMessages = 
            [ "where is foxy"
              "where's foxy"
              "wheres foxy"
              "donde esta foxy"
              "is foxy in dalaran"
              "is foxy in dal"
              "is foxy in moonglade"
              "where's foxy at"
              "can't find foxy"
              "cant find foxy" ]

        if StringUtils.containsAny a whereIsFoxyMessages then 
            Some AsksWhereFoxyIs
        else 
            None 


    let (|Mentioned|NotMentioned|) (msg: IMessage) = 
        let mentionString = formatMentionString client.CurrentUser.Id

        if StringUtils.startsWithAny msg.Content ["!dijon"; mentionString]
        then Mentioned
        else NotMentioned        

    let parseCommand (msg: IMessage): Command = 
        match msg with 
        | NotMentioned -> 
            match msg.Content with 
            | ContainsSlander -> 
                Slander 
            | AsksWhereFoxyIs -> 
                FoxyLocation
            | _ -> 
                Ignore
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
            | "help me out"
            | "help me out here"
            | "back me up"
            | "this is outrageous"
            | "this is ridiculous"
            | "do something"
            | "do something!"
            | "pls"
            | "please"
            | "back me up" -> AidAgainstSlander
            | ContainsSlander -> Slander 
            | AsksWhereFoxyIs -> FoxyLocation
            | _ -> Unknown

    let handleTestMessage (msg: IMessage) (send: IMessageChannel -> GuildUser -> Async<unit>) =
        let fakeUser = {
            Nickname = Some "TestUser"
            UserName = "Discord"
            Discriminator = "0000"
            AvatarUrl = msg.Author.GetAvatarUrl(size = uint16 1024)
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
            // React 1/3 times
            match [1;2;3] |> Seq.randomItem with
            | i when i = 1 ->
                [ "😭"; "💔"; "🔪"; "😡" ]
                |> Seq.randomItem
                |> Emoji 
                |> react (msg :?> SocketUserMessage)
            | _ -> 
                Async.Empty

    let handleFoxyLocation (msg : IMessage) = 
        match msg with 
        | Mentioned -> 
            // Send the Twitch clip embed
            let twitchClipMsg = "Schrodinger's Foxy: he exists in both Dalaran and Moonglade, but you never know which until you pull a boss: https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
            sendMessage msg.Channel twitchClipMsg
        | NotMentioned -> 
            // Send a message 1/3 times
            match [1;2;3;] |> Seq.randomItem with 
            | i when i = 1 -> 
                let twitchClipMsg = "Foxy is _always_ in Dalaran! https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
                sendMessage msg.Channel twitchClipMsg
            | _ -> 
                Async.Empty
            
                
    let handleAidAgainstSlander (msg: IMessage) =
        match msg.Author.Id with
        | i when i = djurId ->
            let responseMsg =
                [ "This bot will not rest until all dissidents have been crushed."
                  sprintf "Down with %s!" (formatMentionString biggelsId)
                  sprintf "Down with %s!" (formatMentionString foxyId)
                  sprintf "Down with %s!" (formatMentionString calyId)
                  "In the land of Djur'alotha, there is only goulash."
                  sprintf "Long may %s reign!" (formatMentionString djurId) ]
                |> Seq.randomItem
            
            [ fun _ -> sendMessage msg.Channel responseMsg
              fun _ -> sendMessage msg.Channel (randomSlanderResponse ()) ]
            |> Async.Sequential
        | _ ->
            // Gifs that say "no" or just laugh
            [ "https://tenor.com/view/monkey-sad-frown-gif-7424667"
              "https://tenor.com/view/cackle-gif-8647880"
              "https://tenor.com/view/evil-laugh-star-wars-sith-palpatine-gif-4145117"
              "https://tenor.com/view/jake-gyllenhaal-smh-shake-my-head-no-nope-gif-4996204" ]
            |> Seq.randomItem
            |> sendMessage msg.Channel

    let handleHypeMessage (msg: IMessage) = 
        let msg = msg :?> SocketUserMessage
        
        match msg.Author.Id with
        | i when i = djurId ->
            let djurHype =
                [ "https://az.nozzlegear.com/images/share/2019-10-23.09.41.19.png"
                  "Here's a glimpse into Djur's average day: https://www.youtube.com/watch?v=hyNu5i_6lKA" ]
                |> Seq.randomItem
               
            let addReactions = fun _ -> 
                ["👌"; "🎉"; "👏"]
                |> Seq.map Emoji
                |> Seq.cast<IEmote>
                |> mutliReact msg
            [
                addReactions
                fun _ -> sendMessage msg.Channel djurHype
            ]
            |> Async.Sequential
        | i when i = foxyId ->
            let foxyHype =
                [ "Boner bear, boner bear, does whatever a boner bear does. 🦴🐻"
                  "Don't forget to do two ready checks whenever Foxy is in the raid! https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
                  // Bear taunt
                  "https://az.nozzlegear.com/images/share/2019-10-23.09.15.02.png"
                  "You know him well! He's the number one member of the Loose Bois team! 🦊"
                  "_Foxy eating spicy food._ https://az.nozzlegear.com/images/share/tenor.gif" ]
                |> Seq.randomItem
           
            sendMessage msg.Channel foxyHype  
        | i when i = calyId ->
            let calyHype =
                [ "https://cdn.discordapp.com/attachments/477977486857338880/601212276984250398/image.png"
                  "Don't let Calyso send you food. https://cdn.discordapp.com/attachments/477977486857338880/585631207824293908/2019-01-11-200450.jpg"
                  "https://cdn.discordapp.com/attachments/477977486857338880/509872721891688458/Snapchat-973247782.jpg" ]
                |> Seq.randomItem
            sendMessage msg.Channel calyHype
        | _ -> 
            ["🇳"; "🇴"]
            |> Seq.map Emoji
            |> Seq.cast<IEmote>
            |> mutliReact msg

    let handleUnknownMessage (msg: IMessage) = 
        let msg = msg :?> SocketUserMessage
        react msg (Emoji "\uD83E\uDD37")

    interface IMessageHandler with 
        member x.HandleMessage msg = 
            let self = x :> IMessageHandler

            match parseCommand msg with 
            | Test -> handleTestMessage msg self.SendUserLeftMessage 
            | Goulash -> handleGoulashRecipe msg 
            | Status -> handleStatusMessage msg 
            | SetLogChannel -> handleSetLogChannelMessage msg 
            | Slander -> handleSlander msg 
            | AidAgainstSlander -> handleAidAgainstSlander msg
            | Help -> handleHelpMessage msg
            | Hype -> handleHypeMessage msg
            | FoxyLocation -> handleFoxyLocation msg
            | Unknown -> handleUnknownMessage msg
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
