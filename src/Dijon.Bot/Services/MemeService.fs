namespace Dijon.Bot.Services

open Dijon.Bot
open Dijon.Shared

open Discord
open Discord.WebSocket
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting

type MemeService(
    bot: IBotClient
) =

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
          "https://tenor.com/view/anakin-darth-vader-gif-5233555"
          "https://cdn.discordapp.com/attachments/974783104713625632/1182004293302226994/oase007_Joe_Biden_with_sunglasses_922c2237-adf4-44b5-ab05-7a68813b6b06.png.jpg"
          "https://cdn.discordapp.com/attachments/974783104713625632/1202718739003609238/your-mother.mp4" ]
        |> Seq.randomItem

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
        |> Task.ignore

    let handleSlander (msg: IMessage) =
        match msg with
        | CommandParser.Mentioned ->
            randomSlanderResponse ()
            |> MessageUtils.sendMessage msg.Channel
        | CommandParser.NotMentioned ->
            // React 1/3 times
            match [1;2;3] |> Seq.randomItem with
            | i when i = 1 ->
                [ "😭"; "💔"; "🔪"; "😡" ]
                |> Seq.randomItem
                |> Emoji
                |> MessageUtils.react (msg :?> SocketUserMessage)
            | _ ->
                Task.empty

    let handleFoxyLocation (msg : IMessage) =
        match msg with
        | CommandParser.Mentioned ->
            // Send the Twitch clip embed
            let twitchClipMsg = "Schrodinger's Foxy: he exists in both Dalaran and Moonglade, but you never know which until you pull a boss: https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
            MessageUtils.sendMessage msg.Channel twitchClipMsg
        | CommandParser.NotMentioned ->
            // Send a message 1/3 times
            match [1;2;3;] |> Seq.randomItem with
            | i when i = 1 ->
                let twitchClipMsg = "Foxy is _always_ in Dalaran! https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
                MessageUtils.sendMessage msg.Channel twitchClipMsg
            | _ ->
                Task.empty

    let handleAidAgainstSlander (msg: IMessage) =
        match msg.Author.Id with
        | i when i = KnownUsers.DjurId ->
            let responseMsg =
                [ "This bot will not rest until all dissidents have been crushed."
                  sprintf "Down with %s!" (MessageUtils.mentionUser KnownUsers.BiggelsId)
                  sprintf "Down with %s!" (MessageUtils.mentionUser KnownUsers.FoxyId)
                  "In the land of Djur'alotha, there is only goulash."
                  sprintf "Long may %s reign!" (MessageUtils.mentionUser KnownUsers.DjurId) ]
                |> Seq.randomItem

            [ MessageUtils.sendMessage msg.Channel responseMsg
              MessageUtils.sendMessage msg.Channel (randomSlanderResponse ()) ]
            |> Task.sequential
            |> Task.toEmpty
        | _ ->
            // Gifs that say "no" or just laugh
            [ "https://tenor.com/view/monkey-sad-frown-gif-7424667"
              "https://tenor.com/view/cackle-gif-8647880"
              "https://tenor.com/view/evil-laugh-star-wars-sith-palpatine-gif-4145117"
              "https://tenor.com/view/jake-gyllenhaal-smh-shake-my-head-no-nope-gif-4996204" ]
            |> Seq.randomItem
            |> MessageUtils.sendMessage msg.Channel

    let handleHypeMessage (hypeTarget: IUser) (channel: IMessageChannel) =
        match hypeTarget.Id with
        | i when i = KnownUsers.DjurId ->
            let djurHype =
                [ "https://az.nozzlegear.com/images/share/2019-10-23.09.41.19.png"
                  "Here's a glimpse into Djur's average day: https://www.youtube.com/watch?v=hyNu5i_6lKA"
                  "If only Djur could pick a class! https://i.imgflip.com/48mqi5.jpg"
                  "If only Djur could pick a class! https://cdn.discordapp.com/attachments/665392948778893333/733877683863289927/48mqln.png"
                  "https://cdn.discordapp.com/attachments/477977486857338880/666298017984544798/ezgif.com-add-text.gif"
                  "https://cdn.discordapp.com/attachments/993576330664878182/1206358940322103326/Biggs_and_Djur_average_day_30fps.mov"
                  $"Live look at {MentionUtils.MentionUser KnownUsers.DjurId} and {MentionUtils.MentionUser KnownUsers.RhunonId} driving their front-wheel drive electric car with summer tires through a blizzard to Starbucks:\n\nhttps://tenor.com/view/you-might-want-to-buckle-up-dominic-toretto-vin-diesel-fast-x-get-ready-gif-2401673552153870491"
                  "No raider is safe from the long arm of the raid leader: https://cdn.discordapp.com/attachments/856354026509434890/878454571033309204/Gripping_Biggelbaalz.mp4" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel djurHype
        | i when i = KnownUsers.FoxyId ->
            let foxyHype =
                [ "Boner bear, boner bear, does whatever a boner bear does. 🦴🐻"
                  "Don't forget to do two ready checks whenever Foxy is in the raid! https://clips.twitch.tv/HyperCrackyRabbitHeyGirl"
                  // Bear taunt
                  "https://az.nozzlegear.com/images/share/2019-10-23.09.15.02.png"
                  "You know him well! He's the number one member of the Loose Bois team! 🦊"
                  "_Foxy eating spicy food._ https://az.nozzlegear.com/images/share/tenor.gif"
                  "Foxy is the kind of bear who would show his left hand. 😼💦"
                  "Live look at Foxy after he swapped to feral: https://cdn.discordapp.com/attachments/856354026509434890/878455897838477362/Angry_Feral_GIF_-_Angry_Feral_Wolf_-_Discover__Share_GIFs_1-angry-feral.mp4" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel foxyHype
        | i when i = KnownUsers.RhunonId ->
            let rhunonHype =
                [ "Hail to the Queen! https://tenor.com/view/rihanna-crown-queen-princess-own-it-gif-4897467"
                  "https://tenor.com/view/were-not-worthy-waynes-world-gif-9201571"
                  $"{MessageUtils.mentionUser KnownUsers.DjurId} would be lost without the Queen."
                  $"I wonder what {MessageUtils.mentionUser KnownUsers.RhunonId} is reading today? https://cdn.discordapp.com/attachments/993576330664878182/1206332634834272306/tiktok.mp4"
                  "https://tenor.com/view/the-outpost-the-outpost-series-thecw-gulman-randall-malin-gif-12842854"
                  "https://cdn.discordapp.com/attachments/856354026509434890/878453670826635324/mistweavers.mp4" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel rhunonHype
        | i when i = KnownUsers.BiggelsId ->
            let biggsHype =
                [ $"{MessageUtils.mentionUser KnownUsers.BiggelsId} is a decidedly okay warlock!"
                  "You're clearly guilty of tyrannicide! https://cdn.discordapp.com/attachments/856354026509434890/878455174799192104/biggs-is-guilty.mp4"
                  "https://cdn.discordapp.com/attachments/993576330664878182/1206358940322103326/Biggs_and_Djur_average_day_30fps.mov"
                  "Feast in five!"
                  "Superbloom in ten!" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel biggsHype
        | i when i = KnownUsers.TazId ->
            let tazHype =
                [ "Taz'dingo! https://tenor.com/view/arrow-hunting-fierce-nature-shoot-gif-14621316"
                  "You've never seen a more exceptional hunter than Taz! https://tenor.com/view/bow-and-arrow-nerd-happy-cd-glasses-gif-15617156" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel tazHype
        | i when i = KnownUsers.InitId ->
            let initHype =
                [ "HEY https://tenor.com/view/spongebob-squarepants-chest-bust-rip-shirt-gif-4172168"
                  "https://youtu.be/txuWGoZF3ew?si=ygVfLFiMyEKraMd6"
                  ":gorillasmirk: :gorillasmirk2: :shortyconcern:"
                  "WWBMD? :bigmike: :gorillasmirk:"
                  $"{MessageUtils.mentionUser KnownUsers.InitId} on a Tinder date: https://cdn.discordapp.com/attachments/974783104713625632/1197344778858270872/Video_by_nutnut_binks_C19IFUcO7E3.mp4"
                  $"{MessageUtils.mentionUser KnownUsers.InitId} explaining why he hasn't painted his walls yet:\n\nhttps://getyarn.io/yarn-clip/ba707bce-ac71-4ce9-ad93-69e53dcd20e2"
                  "https://cdn.discordapp.com/attachments/974783104713625632/1202718739003609238/your-mother.mp4" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel initHype
        | i when i = KnownUsers.DemiId ->
            let demiHype =
                [ "https://cdn.discordapp.com/attachments/974783104713625632/1202718739003609238/your-mother.mp4"
                  $"When {MessageUtils.mentionUser KnownUsers.DemiId} and {MessageUtils.mentionUser KnownUsers.DjurId} play 3s:\n\nhttps://tenor.com/view/toaster-lights-on-and-off-bathtub-confused-bill-murray-gif-10874237"
                  $"When {MessageUtils.mentionUser KnownUsers.DemiId} and {MessageUtils.mentionUser KnownUsers.DjurId} play 3s:\n\nhttps://tenor.com/view/kill-me-drink-bleach-gif-22890796"
                  $"{MessageUtils.mentionUser KnownUsers.DemiId}'s next vehicle:\n\nhttps://www.youtube.com/watch?v=MI7Tq6sRxE4"
                  $"Live look at {MessageUtils.mentionUser KnownUsers.DemiId}'s wife jumping into the Fury Mobile: https://cdn.discordapp.com/attachments/993576330664878182/1206339975445221436/2022-08-28_11.37.42.mov"
                  $"{MessageUtils.mentionUser KnownUsers.DemiId}'s arena strategy: https://cdn.discordapp.com/attachments/993576330664878182/1206340178981953626/2022-07-17_23.06.17.mov"
                  ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel demiHype
        | i when i = KnownUsers.DurzId ->
            let durzHype =
                [ "Live look at Durz when he casts Hand of Freedom:\n\nhttps://tenor.com/view/speed-wheelchair-me-running-late-gif-14178485"
                  "https://tenor.com/view/george-costanza-scooter-look-back-slow-gif-14470443"
                  $"When {MessageUtils.mentionUser KnownUsers.DurzId} tells us he's done with WoW: https://cdn.discordapp.com/attachments/993576330664878182/1206332994629935265/Video_by_wowentertainer_CzWRBdyNoVb.mp4"
                  "https://tenor.com/view/wheelchair-fall-fail-gif-8902077" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel durzHype
        | i when i = KnownUsers.BopittId ->
            let bopittHype =
                [ "https://cdn.discordapp.com/attachments/856354026509434890/878451088905342996/bopitt.mp4.mp4"
                  "https://cdn.discordapp.com/attachments/993576330664878182/1206319980954652732/spoopz_r2d2.mp3"
                  "https://www.youtube.com/watch?v=jxo_K7JLZxQ" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel bopittHype
        | i when i = KnownUsers.BoroId ->
            let boroHype =
                [ "He's got the motherfuckin' Halo theme song playing, LETS GOOOOO!\n\nhttps://www.youtube.com/watch?v=sCxv2daOwjQ"
                  "https://cdn.discordapp.com/attachments/993576330664878182/1206319981260832788/boro-oh-convoke.wav" ]
                |> Seq.randomItem

            MessageUtils.sendMessage channel boroHype
        | _ ->
            let message =
                // Respond with image 1/5 times
                match Seq.randomItem [1; 2; 3; 4; 5] with
                | 1 ->
                    let img = "https://cdn.discordapp.com/attachments/974783104713625632/1182004293302226994/oase007_Joe_Biden_with_sunglasses_922c2237-adf4-44b5-ab05-7a68813b6b06.png.jpg"
                    $"{MessageUtils.mentionUser hypeTarget.Id} I don't think so, Corn Pop.\n\n{img}"
                | _ ->
                    $"{MessageUtils.mentionUser hypeTarget.Id} No."

            MessageUtils.sendMessage channel message

    let handleCommand (msg : IMessage) = function
        | Goulash -> handleGoulashRecipe msg
        | Slander -> handleSlander msg
        | AidAgainstSlander -> handleAidAgainstSlander msg
        | Hype -> Task.toEmpty (handleHypeMessage msg.Author msg.Channel)
        | FoxyLocation -> handleFoxyLocation msg
        | _ -> Task.empty

    let buildHypeCommand () =
        let hypeTargetOption =
            SlashCommandOptionBuilder()
                .WithName("target")
                .WithDescription("Unleash the hypebeast on an unsuspecting target by @'ing them.")
                .WithType(ApplicationCommandOptionType.User)
                .WithRequired(false)
        SlashCommandBuilder()
            .WithName("hype")
            .WithDescription("Having a bad day? Let Dijon cheer you up! Results may vary.")
            .WithDMPermission(false)
            .WithDefaultPermission(true)
            .AddOption(hypeTargetOption)
            .Build()

    let handleHypeCommand (command: SocketSlashCommand) =
        match command.CommandName with
        | "hype" ->
            task {
                let target = command.Data.Options
                             |> Seq.tryFind (fun x -> x.Name = "target")
                             |> Option.map (fun x -> x.Value :?> IUser)
                             |> Option.defaultValue command.User

                do! handleHypeMessage target command.Channel
            }
        | _ ->
            task {
                do! command.RespondAsync($"This bot doesn't recognize the command \"{command.CommandName}\", please stop being cringe.")
            }

    interface IDisposable with
        member _.Dispose() =
            ()

    interface IHostedService with
        member _.StartAsync _ =
            bot.AddEventListener (DiscordEvent.CommandReceived handleCommand)
            bot.AddEventListener (DiscordEvent.SlashCommandExecuted handleHypeCommand)
            bot.RegisterCommands [ buildHypeCommand () ]

        member _.StopAsync _ =
            Task.CompletedTask
