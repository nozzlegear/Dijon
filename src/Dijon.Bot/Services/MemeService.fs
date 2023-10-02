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
          "https://tenor.com/view/anakin-darth-vader-gif-5233555" ]
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

    let handleHypeMessage (msg: IMessage) = 
        let msg = msg :?> SocketUserMessage
        
        match msg.Author.Id with
        | i when i = KnownUsers.DjurId ->
            let djurHype =
                [ "https://az.nozzlegear.com/images/share/2019-10-23.09.41.19.png"
                  "Here's a glimpse into Djur's average day: https://www.youtube.com/watch?v=hyNu5i_6lKA"
                  "If only Djur could pick a class! https://i.imgflip.com/48mqi5.jpg"
                  "If only Djur could pick a class! https://cdn.discordapp.com/attachments/665392948778893333/733877683863289927/48mqln.png"
                  "https://cdn.discordapp.com/attachments/477977486857338880/666298017984544798/ezgif.com-add-text.gif"
                  "STAMINA STAVES ARE LEGITIMATE BREWMASTER WEAPONS, WHO CARES IF THEY DON'T HAVE AGILITY?!"
                  "No raider is safe from the long arm of the raid leader: https://cdn.discordapp.com/attachments/856354026509434890/878454571033309204/Gripping_Biggelbaalz.mp4" ]
                |> Seq.randomItem
               
            let addReactions =
                ["👌"; "🎉"; "👏"]
                |> Seq.map Emoji
                |> Seq.cast<IEmote>
                |> MessageUtils.multiReact msg
            [
                addReactions
                MessageUtils.sendMessage msg.Channel djurHype
            ]
            |> Task.sequential
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
           
            MessageUtils.sendMessage msg.Channel foxyHype  
        | i when i = KnownUsers.RhunonId ->
            let rhunonHype =
                [ "Hail to the Queen! https://tenor.com/view/rihanna-crown-queen-princess-own-it-gif-4897467"
                  "https://tenor.com/view/were-not-worthy-waynes-world-gif-9201571"
                  sprintf "%swould be lost without the Queen." (MessageUtils.mentionUser KnownUsers.DjurId)
                  "https://tenor.com/view/the-outpost-the-outpost-series-thecw-gulman-randall-malin-gif-12842854"
                  "https://cdn.discordapp.com/attachments/856354026509434890/878453670826635324/mistweavers.mp4" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel rhunonHype
        | i when i = KnownUsers.BiggelsId ->
            let biggsHype =
                [ "Let your failure be the final word in the story of rebellion! https://tenor.com/view/palpatine-the-rise-of-skywalker-lightning-palpatine-lightning-exegol-gif-18167689"
                  sprintf "%sis a decidedly okay healer!" (MessageUtils.mentionUser KnownUsers.BiggelsId)
                  "JUST STAND IN THE MIDDLE AND HEAL THROUGH IT https://tenor.com/view/georffrey-rush-captain-of-the-ship-is-giving-orders-barbossa-pirates-of-the-caribbean-gif-9227393"
                  "Although he masquerades as the architect of #DownWithDjur, we all know he's secretly in the benevolent leader's pocket!"
                  "You're clearly guilty of tyrannicide! https://cdn.discordapp.com/attachments/856354026509434890/878455174799192104/biggs-is-guilty.mp4" ]
                |> Seq.randomItem
            
            MessageUtils.sendMessage msg.Channel biggsHype
        | i when i = KnownUsers.TazId ->
            let tazHype =
                [ "Taz'dingo! https://tenor.com/view/arrow-hunting-fierce-nature-shoot-gif-14621316"
                  "You've never seen a more exceptional hunter than Taz! https://tenor.com/view/bow-and-arrow-nerd-happy-cd-glasses-gif-15617156" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel tazHype
        | i when i = KnownUsers.InitId ->
            let initHype =
                [ "HEY https://tenor.com/view/spongebob-squarepants-chest-bust-rip-shirt-gif-4172168"
                  "https://tenor.com/view/happy-im-so-happy-happiness-joy-excited-gif-16119788"
                  "Init be like: https://tenor.com/view/pancakes-michael-scott-you-will-like-it-food-gif-16324949"
                  "He's the original member of the Pancake Party! https://tenor.com/view/bunny-pancakes-wreck-it-ralph-gif-11221126"
                  "TOMATO SHRAPNEL IS REAL! THIS BOT HAS SEEN THE TRUTH!" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel initHype
        | i when i = KnownUsers.DurzId ->
            let durzHype =
                [ "Live look at Durz when he casts Hand of Freedom: https://tenor.com/view/speed-wheelchair-me-running-late-gif-14178485"
                  "https://tenor.com/view/george-costanza-scooter-look-back-slow-gif-14470443"
                  "https://tenor.com/view/wheelchair-fall-fail-gif-8902077" ]
                |> Seq.randomItem
                
            MessageUtils.sendMessage msg.Channel durzHype
        | i when i = KnownUsers.BopittId ->
            let bopittHype =
                [ "https://cdn.discordapp.com/attachments/856354026509434890/878451088905342996/bopitt.mp4.mp4"
                  "https://www.youtube.com/watch?v=jxo_K7JLZxQ" ]
                |> Seq.randomItem

            MessageUtils.sendMessage msg.Channel bopittHype
        | i when i = KnownUsers.BoroId ->
            let boroHype =
                [ "He's got the motherfuckin' Halo theme song playing, LETS GOOOOO! https://www.youtube.com/watch?v=sCxv2daOwjQ" ]
                |> Seq.randomItem

            MessageUtils.sendMessage msg.Channel boroHype
        | _ -> 
            ["🇳"; "🇴"]
            |> Seq.map Emoji
            |> Seq.cast<IEmote>
            |> MessageUtils.multiReact msg

    let handleCommand (msg : IMessage) = function
        | Goulash -> handleGoulashRecipe msg 
        | Slander -> handleSlander msg 
        | AidAgainstSlander -> handleAidAgainstSlander msg
        | Hype -> Task.toEmpty (handleHypeMessage msg)
        | FoxyLocation -> handleFoxyLocation msg
        | _ -> Task.empty

    interface IDisposable with
        member _.Dispose() = 
            ()

    interface IHostedService with
        member _.StartAsync _ =
            bot.AddEventListener (DiscordEvent.CommandReceived handleCommand)
            Task.CompletedTask

        member _.StopAsync _ =
            Task.CompletedTask
