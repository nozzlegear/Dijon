namespace Dijon.Services

open Dijon
open Discord
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive

type ReactionGuardService(logger : ILogger<ReactionGuardService>, 
                 bot : Dijon.BotClient, 
                 database : IDijonDatabase) =

    let whitelist = [
        "❤️"
        "♥️"
        "❤️‍🔥"
        "🖤"
        "💙"
        "🤎"
        "💚"
        "💜"
        "💛"
        "💓"
        "💞"
        "💖"
        "💕"
        "💝"
        "❤️‍🩹"
        "💘"
        "😻"
        "🥰"
        "😍"
        "😘"
        "🤗"
        "🍻"
        "🎉"
        "👍"
        "👏"
        "💪"
        "💯"
        "🌈"
        "🏳️‍🌈"
        "🏳️‍⚧️"
        "rainbowbear"
        "rainbowhorde"
        "happybear"
        "HapyBear"
        "my_man"
        "sogud"
        "100rogue"
        "toastleft"
        "toastright"
        "perfect"
        "tde"
        "jaiyeah"
    ]

    let handleReaction (msg : CachedUserMessage) (channel: IChannel) (reaction: IReaction) =  async {
        let reactionIsWhitelisted = 
            List.contains reaction.Emote.Name whitelist

        if not reactionIsWhitelisted then
            match! database.MessageIsReactionGuarded (int64 msg.Id) with
            | true ->
                logger.LogInformation("Emote {0} is not whitelisted, removing", reaction.Emote.Name)
                do! bot.RemoveAllReactionsForEmoteAsync(channel.Id, msg.Id, reaction.Emote)
            | false -> 
                ()
    }

    let addReactionGuard (msg: IMessage) =
        let channel = msg.Channel :?> IGuildChannel
        // If the command message references (e.g. replies to) a different message, guard that message
        let guard : ReferencedMessage = 
            MessageUtils.GetReferencedMessage msg
            |> Option.defaultValue (
                {
                    GuildId = int64 channel.GuildId
                    ChannelId = int64 msg.Channel.Id
                    MessageId = int64 msg.Id
                })

        async {
            do! database.AddReactionGuardedMessage guard
            do! MessageUtils.AddGreenCheckReaction msg
        }

    let removeReactionGuard (msg: IMessage) = async {
        let reference = MessageUtils.GetReferencedMessage msg

        if Option.isSome reference then
            let reference = Option.get reference
            do! database.RemoveReactionGuardedMessage (int64 reference.MessageId)
            do! MessageUtils.AddGreenCheckReaction msg
        else
            do! MessageUtils.AddXReaction msg
    }

    let commandReceived (msg: IMessage) = function
        | Command.AddMessageReactionGuard -> 
            addReactionGuard msg
        | Command.RemoveMessageReactionGuard ->
            removeReactionGuard msg
        | _ ->
            Async.Empty

    interface IDisposable with
        member _.Dispose() = 
            ()

    interface IHostedService with
        member _.StartAsync cancellation =
            task {
                do! bot.AddEventListener (DiscordEvent.ReactionReceived handleReaction)
                do! bot.AddEventListener (DiscordEvent.CommandReceived commandReceived)
            }
            :> Task

        member _.StopAsync _ =
            Task.CompletedTask
