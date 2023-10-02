namespace Dijon.Bot.Services

open Dijon.Bot
open Dijon.Shared
open Dijon.Database.MessageReactionGuards

open Discord
open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type ReactionGuardService(
    logger: ILogger<ReactionGuardService>,
    bot: IBotClient,
    database: IMessageReactionGuardDatabase
) =

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
        "✅"
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
        "gingerscheme"
    ]

    let handleReaction (msg : CachedUserMessage) (channel: IChannel) (reaction: IReaction) = task {
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
            |> Option.defaultValue
                { GuildId = int64 channel.GuildId
                  ChannelId = int64 msg.Channel.Id
                  MessageId = int64 msg.Id }

        task {
            do! database.AddReactionGuardedMessage guard
            do! MessageUtils.AddGreenCheckReaction msg
        }

    let removeReactionGuard (msg: IMessage) = task {
        let isAdmin = List.contains msg.Author.Id KnownUsers.AdminUsers

        if not isAdmin then
            do! MessageUtils.Reply msg "Only admin users can remove a reaction guard."
        else
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
            Task.empty

    interface IDisposable with
        member _.Dispose() =
            ()

    interface IHostedService with
        member _.StartAsync _ =
            bot.AddEventListener (DiscordEvent.ReactionReceived handleReaction)
            bot.AddEventListener (DiscordEvent.CommandReceived commandReceived)
            Task.CompletedTask

        member _.StopAsync _ =
            Task.CompletedTask
