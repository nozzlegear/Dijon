namespace Dijon.Bot

open Dijon.Database.MessageReactionGuards
open Dijon.Shared

open System
open Discord
open Discord.WebSocket

module MessageUtils =

    let mentionUser (userId : uint64) = sprintf "<@%i> " userId

    let embedField title value = EmbedFieldBuilder().WithName(title).WithValue(value)

    let sendEditableMessage (channel : IMessageChannel) msg = channel.SendMessageAsync msg |> Async.AwaitTask

    let sendMessage (channel: IMessageChannel) msg = sendEditableMessage channel msg |> Async.Ignore

    let sendEmbed (channel: IMessageChannel) (embed: EmbedBuilder) = 
        async {
            let! result = channel.SendMessageAsync("", false, embed.Build()) 
                          |> Async.AwaitTask

            return int64 result.Id
        }

    let react (msg: SocketUserMessage) emote = 
        msg.AddReactionAsync emote 
        |> Async.AwaitTask

    let multiReact (msg: SocketUserMessage) (emotes: IEmote seq) = 
        emotes
        |> Seq.map (fun e -> fun _ -> react msg e)
        |> Async.Sequential

    let AddGreenCheckReaction (msg : IMessage) =
        msg.AddReactionAsync (Emoji "✅")
        |> Async.AwaitTask

    let AddShrugReaction (msg : IMessage) = 
        msg.AddReactionAsync (Emoji "🤷")
        |> Async.AwaitTask

    /// Adds the ❌ emoji reaction to the message. Generally used when user input is invalid.
    let AddXReaction (msg : IMessage) =
        msg.AddReactionAsync (Emoji "❌")
        |> Async.AwaitTask

    /// Get's the user's Nickname if available, else their Discord username.
    let GetNickname (user : IUser) = 
        match user with
        | :? IGuildUser as guildUser when not (String.IsNullOrEmpty guildUser.Nickname) -> guildUser.Nickname
        | _ -> user.Username

    /// Maps a message's reference (i.e. the original message if this was a reply) to an option
    let GetReferencedMessage (msg: IMessage) : ReferencedMessage option = 
        Option.ofObj msg.Reference
        |> Option.map (fun reference -> 
            {
                GuildId = int64 <| reference.GuildId.GetValueOrDefault()
                ChannelId = int64 reference.ChannelId
                MessageId = int64 <| reference.MessageId.GetValueOrDefault()
            })

    let Reply (msg : IMessage) reply =
        let msg = msg :?> SocketUserMessage
        msg.ReplyAsync(text = reply, allowedMentions = AllowedMentions.None)
        |> Async.AwaitTask
        |> Async.Ignore
