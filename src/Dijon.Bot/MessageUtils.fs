namespace Dijon.Bot

open Dijon.Database.MessageReactionGuards
open Dijon.Shared

open System.Threading.Tasks
open System
open Discord
open Discord.WebSocket

module MessageUtils =

    let mentionUser (userId : uint64) = sprintf "<@%i> " userId

    let embedField title value = EmbedFieldBuilder().WithName(title).WithValue(value)

    let sendEditableMessage (channel : IMessageChannel) msg =
        channel.SendMessageAsync msg

    let sendMessage (channel: IMessageChannel) msg =
        sendEditableMessage channel msg
        |> Task.ignore

    let sendEmbed (channel: IMessageChannel) (embed: EmbedBuilder) = 
        channel.SendMessageAsync("", false, embed.Build())
        |> Task.map (fun x -> int64 x.Id)

    let react (msg: SocketUserMessage) emote = 
        msg.AddReactionAsync emote
        |> Task.toEmpty

    let multiReact (msg: SocketUserMessage) (emotes: IEmote seq) = 
        emotes
        |> Seq.map (react msg)
        |> Task.sequential

    let AddGreenCheckReaction (msg : IMessage) =
        msg.AddReactionAsync (Emoji "✅")
        |> Task.toEmpty

    let AddShrugReaction (msg : IMessage) = 
        msg.AddReactionAsync (Emoji "🤷")
        |> Task.toEmpty

    /// Adds the ❌ emoji reaction to the message. Generally used when user input is invalid.
    let AddXReaction (msg : IMessage) =
        msg.AddReactionAsync (Emoji "❌")
        |> Task.toEmpty

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
        |> Task.ignore
