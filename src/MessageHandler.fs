namespace Dijon 

open Dijon.RaiderIo
open Discord
open System
open Discord.WebSocket
open Microsoft.Extensions.Logging

type MessageHandler(logger: ILogger<MessageHandler>, database: IDijonDatabase, bot: BotClient) = 
    let djurId = uint64 204665846386262016L
    let foxyId = uint64 397255457862975509L
    let calyId = uint64 148990194815598592L
    let biggelsId = uint64 479036597312946177L
    let rhunonId = uint64 376795664698441740L
    let tazId = uint64 223974005948809223L
    let initId = uint64 162728562850267138L
    let durzId = uint64 241044004269981697L
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

    let (|ContainsSlander|_|) (a: string) = 
        let tdeDownWithDjurChannel = 561289801890791425L
        let slanderMessages = 
            [ "#downwithdjur" 
              "down with djur"
              ":downwithdjur:"
              sprintf "<#%i>" tdeDownWithDjurChannel ]

        if String.containsAny a slanderMessages then
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

        if String.containsAny a whereIsFoxyMessages then 
            Some AsksWhereFoxyIs
        else 
            None 

    let (|Mentioned|NotMentioned|) (msg: IMessage) = 
        let mentionString = 
            bot.GetBotUserId ()
            |> MessageUtils.mentionUser 

        if String.startsWithAny msg.Content ["!dijon"; mentionString]
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
            match Messages.SanitizeForParsing msg with 
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
            | "set affixes here"
            | "set affixes"
            | "affixes here"
            | "set affix channel here"
            | "affix here"
            | "set affix channel"
            | "set affixes channel" -> SetAffixesChannel
            | "set stream channel"
            | "set streams channel"
            | "set stream announcement channel"
            | "set stream announcements channel"
            | "set streams here"
            | "set stream announcements channel" 
            | "set stream announcements channel"
            | "set stream announcements channel to"
            | "set streams"
            | "set streams to"
            | "set streams for"
            | "set streams in"
            | "set streams in for"
            | "set streams for in"
            | "announce streams for"
            | "announce streams for in"
            | "announce streams" -> SetStreamsChannel
            | "affix"
            | "what are the affixes"
            | "affixes" -> GetAffix
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
            "- 1lb ground beef or plant meat"
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
            MessageUtils.embedField "Ingredients" (String.newlineJoin ingredients)
            MessageUtils.embedField "Instructions" (String.newlineJoin instructions)
        ]

        MessageUtils.sendEmbed msg.Channel embed 

    let handleStatusMessage (msg: IMessage) =
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
            }
        | _ -> 
            MessageUtils.sendEmbed msg.Channel embed

    let handleSetLogChannelMessage (msg: IMessage) = 
        match msg.Channel with 
        | :? SocketGuildChannel as guildChannel -> 
            if msg.Author.Id <> djurId then
                MessageUtils.sendMessage msg.Channel (sprintf "At the moment, only the Almighty <@%i> may set the log channel." djurId)
            else 
                async {
                    let guildId = GuildId (int64 guildChannel.Guild.Id)

                    do! database.SetLogChannelForGuild guildId (int64 msg.Channel.Id)
                    do! MessageUtils.sendMessage msg.Channel "Messages will be sent to this channel when a user leaves the server."
                }
        | :? ISocketPrivateChannel -> 
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in a private message."
        | _ -> 
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in unknown channel type."
            
    let handleSetAffixesChannelMessage (msg: IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            if msg.Author.Id <> djurId then
                MessageUtils.sendMessage msg.Channel (sprintf "At the moment, only <@%i> may set the affixes channel." djurId)
            else
                async {
                    let guildId = GuildId (int64 guildChannel.Guild.Id)
                    
                    do! database.SetAffixesChannelForGuild guildId (int64 msg.Channel.Id)
                    do! MessageUtils.sendMessage msg.Channel "Affixes will be sent to this channel every Tuesday."
                }
        | :? ISocketPrivateChannel ->
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in a private message."
        | _ ->
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in unknown channel type."

    let handleSetStreamsChannelMessage (msg : IMessage) =
        match msg.Channel with
        | :? SocketGuildChannel as guildChannel ->
            if Seq.length msg.MentionedRoleIds <> 1 then
                MessageUtils.sendMessage msg.Channel "You must mention exactly 1 role to use as the streamer role."
            elif Seq.length msg.MentionedChannelIds <> 1 then
                MessageUtils.sendMessage msg.Channel "You must mention exactly 1 channel to use as the announcements channel."
            else
                let streamerRoleId = Seq.head msg.MentionedRoleIds
                let channelId = Seq.head msg.MentionedChannelIds
                let guildId = guildChannel.Guild.Id

                async {
                    let channel = 
                        { ChannelId = int64 channelId
                          GuildId = int64 guildId
                          StreamerRoleId = int64 streamerRoleId }

                    do! database.SetStreamAnnouncementChannelForGuild channel

                    let returnMessage = 
                        sprintf "Stream announcement messages from streamers with the role "
                        + MentionUtils.MentionRole streamerRoleId
                        + " will be sent to the channel "
                        + MentionUtils.MentionChannel channelId
                        + "."

                    return! MessageUtils.sendMessage msg.Channel returnMessage
                }
        | :? ISocketPrivateChannel ->
            MessageUtils.sendMessage msg.Channel "Unable to set streams channel in a private message."
        | _ ->
            MessageUtils.sendMessage msg.Channel "Unable to set log channel in unknown channel type."

    let handleHelpMessage (msg: IMessage) =
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

    let handleSlander (msg: IMessage) = 
        match msg with 
        | Mentioned -> 
            randomSlanderResponse ()
            |> MessageUtils.sendMessage msg.Channel
        | NotMentioned ->
            // React 1/3 times
            match [1;2;3] |> Seq.randomItem with
            | i when i = 1 ->
                [ "😭"; "💔"; "🔪"; "😡" ]
                |> Seq.randomItem
                |> Emoji 
                |> MessageUtils.react (msg :?> SocketUserMessage)
            | _ -> 
                Async.Empty

    let handleFoxyLocation (msg : IMessage) = 
        match msg with 
        | Mentioned -> 
            // Send the Twitch clip embed
            let twitchClipMsg = "Schrodinger's Foxy: he exists in both Dalaran and Moonglade, but you never know which until you pull a boss: https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
            MessageUtils.sendMessage msg.Channel twitchClipMsg
        | NotMentioned -> 
            // Send a message 1/3 times
            match [1;2;3;] |> Seq.randomItem with 
            | i when i = 1 -> 
                let twitchClipMsg = "Foxy is _always_ in Dalaran! https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
                MessageUtils.sendMessage msg.Channel twitchClipMsg
            | _ -> 
                Async.Empty
            
                
    let handleAidAgainstSlander (msg: IMessage) =
        match msg.Author.Id with
        | i when i = djurId ->
            let responseMsg =
                [ "This bot will not rest until all dissidents have been crushed."
                  sprintf "Down with %s!" (MessageUtils.mentionUser biggelsId)
                  sprintf "Down with %s!" (MessageUtils.mentionUser foxyId)
                  "In the land of Djur'alotha, there is only goulash."
                  sprintf "Long may %s reign!" (MessageUtils.mentionUser djurId) ]
                |> Seq.randomItem
            
            [ fun _ -> MessageUtils.sendMessage msg.Channel responseMsg
              fun _ -> MessageUtils.sendMessage msg.Channel (randomSlanderResponse ()) ]
            |> Async.Sequential
        | _ ->
            // Gifs that say "no" or just laugh
            [ "https://tenor.com/view/monkey-sad-frown-gif-7424667"
              "https://tenor.com/view/cackle-gif-8647880"
              "https://tenor.com/view/evil-laugh-star-wars-sith-palpatine-gif-4145117"
              "https://tenor.com/view/jake-gyllenhaal-smh-shake-my-head-no-nope-gif-4996204" ]
            |> Seq.randomItem
            |> MessageUtils.sendMessage msg.Channel

    let handleHypeMessage (msg: IMessage) = 
        let msg = msg :?> SocketUserMessage
        
        match msg.Author.Id with
        | i when i = djurId ->
            let djurHype =
                [ "https://az.nozzlegear.com/images/share/2019-10-23.09.41.19.png"
                  "Here's a glimpse into Djur's average day: https://www.youtube.com/watch?v=hyNu5i_6lKA"
                  "If only Djur could pick a class! https://i.imgflip.com/48mqi5.jpg"
                  "If only Djur could pick a class! https://cdn.discordapp.com/attachments/665392948778893333/733877683863289927/48mqln.png"
                  "https://cdn.discordapp.com/attachments/477977486857338880/666298017984544798/ezgif.com-add-text.gif"
                  "STAMINA STAVES ARE LEGITIMATE BREWMASTER WEAPONS, WHO CARES IF THEY DON'T HAVE AGILITY?!" ]
                |> Seq.randomItem
               
            let addReactions = fun _ -> 
                ["👌"; "🎉"; "👏"]
                |> Seq.map Emoji
                |> Seq.cast<IEmote>
                |> MessageUtils.multiReact msg
            [
                addReactions
                fun _ -> MessageUtils.sendMessage msg.Channel djurHype
            ]
            |> Async.Sequential
        | i when i = foxyId ->
            let foxyHype =
                [ "Boner bear, boner bear, does whatever a boner bear does. 🦴🐻"
                  "Don't forget to do two ready checks whenever Foxy is in the raid! https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
                  // Bear taunt
                  "https://az.nozzlegear.com/images/share/2019-10-23.09.15.02.png"
                  "You know him well! He's the number one member of the Loose Bois team! 🦊"
                  "_Foxy eating spicy food._ https://az.nozzlegear.com/images/share/tenor.gif"
                  "Foxy is the kind of bear who would show his right hand. 😼💦" ]
                |> Seq.randomItem
           
            MessageUtils.sendMessage msg.Channel foxyHype  
        | i when i = calyId ->
            let calyHype =
                [ "https://cdn.discordapp.com/attachments/477977486857338880/601212276984250398/image.png"
                  "Don't let Calyso send you food. https://cdn.discordapp.com/attachments/477977486857338880/585631207824293908/2019-01-11-200450.jpg"
                  "https://cdn.discordapp.com/attachments/477977486857338880/509872721891688458/Snapchat-973247782.jpg" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel calyHype
        | i when i = rhunonId ->
            let rhunonHype =
                [ "https://tenor.com/view/rihanna-crown-queen-princess-own-it-gif-4897467"
                  "https://tenor.com/view/jon-snow-my-queen-gif-9619999"
                  "https://tenor.com/view/were-not-worthy-waynes-world-gif-9201571"
                  "🎉 Hail to the Queen! 👑"
                  sprintf "%swould be lost without the Queen." (MessageUtils.mentionUser djurId)
                  "https://tenor.com/view/the-outpost-the-outpost-series-thecw-gulman-randall-malin-gif-12842854" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel rhunonHype
        | i when i = biggelsId ->
            let biggsHype =
                [ "Let your failure be the final word in the story of rebellion! https://tenor.com/view/palpatine-the-rise-of-skywalker-lightning-palpatine-lightning-exegol-gif-18167689"
                  sprintf "%sis a decidedly okay healer!" (MessageUtils.mentionUser biggelsId)
                  "JUST STAND IN THE MIDDLE AND HEAL THROUGH IT https://tenor.com/view/georffrey-rush-captain-of-the-ship-is-giving-orders-barbossa-pirates-of-the-caribbean-gif-9227393"
                  "Although he masquerades as the architect of #DownWithDjur, we all know he's secretly in the benevolent leader's pocket!" ]
                |> Seq.randomItem
            
            MessageUtils.sendMessage msg.Channel biggsHype
        | i when i = tazId ->
            let tazHype =
                [ "Taz'dingo! https://tenor.com/view/arrow-hunting-fierce-nature-shoot-gif-14621316"
                  "You've never seen a more exceptional hunter than Taz! https://tenor.com/view/bow-and-arrow-nerd-happy-cd-glasses-gif-15617156" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel tazHype
        | i when i = initId ->
            let initHype =
                [ "HEY https://tenor.com/view/spongebob-squarepants-chest-bust-rip-shirt-gif-4172168"
                  "https://tenor.com/view/happy-im-so-happy-happiness-joy-excited-gif-16119788"
                  "Init be like: https://tenor.com/view/pancakes-michael-scott-you-will-like-it-food-gif-16324949"
                  "He's the original member of the Pancake Party! https://tenor.com/view/bunny-pancakes-wreck-it-ralph-gif-11221126" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel initHype
        | i when i = durzId ->
            let durzHype =
                [ "Live look at Durz when he casts Hand of Freedom: https://tenor.com/view/speed-wheelchair-me-running-late-gif-14178485"
                  "https://tenor.com/view/george-costanza-scooter-look-back-slow-gif-14470443"
                  "https://tenor.com/view/wheelchair-fall-fail-gif-8902077" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel durzHype
        | _ -> 
            ["🇳"; "🇴"]
            |> Seq.map Emoji
            |> Seq.cast<IEmote>
            |> MessageUtils.multiReact msg
            
    let createAffixesEmbed (affixes: ListAffixesResponse) =
        let builder = EmbedBuilder()
        builder.Color <- Nullable Color.Green
        builder.Title <- sprintf "This week's Mythic+ affixes: %s" affixes.title
        
        affixes.affix_details
        |> List.map (fun affix -> MessageUtils.embedField (sprintf "**%s**" affix.name) affix.description)
        |> builder.Fields.AddRange
        
        builder
            
    let handleGetAffixMessage (msg : IMessage) =
        async {
            let! editable = MessageUtils.sendEditableMessage msg.Channel "Fetching affixes, please wait..."
            let! affixList = Affixes.list()
            let editMessage (props : MessageProperties) =
                let embed =
                    let builder = EmbedBuilder()
                    match affixList with
                    | Error err ->
                        builder.Color <- Nullable Color.Red
                        builder.Title <- "❌ Error fetching affixes!"
                        
                        MessageUtils.embedField "🔬 Details" err
                        |> builder.Fields.Add
                        
                        builder.Build()
                    | Ok affixes ->
                        let embed = createAffixesEmbed affixes
                        embed.Build()
                    
                // Clear the content and add an embed
                props.Content <- Optional.Create ""
                props.Embed <- Optional.Create embed
                    
            do! editable.ModifyAsync (Action<MessageProperties> editMessage) |> Async.AwaitTask
        }

    let handleUnknownMessage (msg: IMessage) = 
        let msg = msg :?> SocketUserMessage
        MessageUtils.react msg (Emoji "\uD83E\uDD37")

    interface IMessageHandler with 
        member x.HandleMessage msg = 
            let self = x :> IMessageHandler
            let job = 
                match parseCommand msg with 
                | Test -> handleTestMessage msg self.SendUserLeftMessage 
                | Goulash -> handleGoulashRecipe msg 
                | Status -> handleStatusMessage msg 
                | SetLogChannel -> handleSetLogChannelMessage msg
                | SetAffixesChannel -> handleSetAffixesChannelMessage msg
                | SetStreamsChannel -> handleSetStreamsChannelMessage msg
                | Slander -> handleSlander msg 
                | AidAgainstSlander -> handleAidAgainstSlander msg
                | Help -> handleHelpMessage msg
                | Hype -> handleHypeMessage msg
                | FoxyLocation -> handleFoxyLocation msg
                | GetAffix -> handleGetAffixMessage msg
                | Unknown -> handleUnknownMessage msg
                | Ignore -> Async.Empty
            
            async {
                match! Async.Catch job with
                | Choice1Of2 _ -> 
                    ()
                | Choice2Of2 ex ->
                    logger.LogError(ex, "Failed to handle message")
                    raise ex
            }

        member x.SendUserLeftMessage channel user = 
            let nickname = Option.defaultValue user.UserName user.Nickname
            let message = sprintf "**%s** (%s#%s) has left the server." nickname user.UserName user.Discriminator
            let embed = EmbedBuilder()
            embed.Title <- "👋"
            embed.Description <- message
            embed.Color <- Nullable Color.DarkOrange
            embed.ThumbnailUrl <- user.AvatarUrl
            
            MessageUtils.sendEmbed channel embed 

        member x.SendAffixesMessage channel affixes =
            let embed = createAffixesEmbed affixes
            
            MessageUtils.sendEmbed channel embed 
