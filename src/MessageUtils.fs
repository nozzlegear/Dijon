namespace Dijon

open Discord
open Discord.WebSocket

module MessageUtils =

    let mentionUser (userId : uint64) = sprintf "<@%i> " userId

    let embedField title value = EmbedFieldBuilder().WithName(title).WithValue(value)

    let sendEditableMessage (channel : IMessageChannel) msg = channel.SendMessageAsync msg |> Async.AwaitTask

    let sendMessage (channel: IMessageChannel) msg = sendEditableMessage channel msg |> Async.Ignore

    let sendEmbed (channel: IMessageChannel) (embed: EmbedBuilder) = channel.SendMessageAsync("", false, embed.Build()) |> Async.AwaitTask |> Async.Ignore

    let react (msg: SocketUserMessage) emote = msg.AddReactionAsync emote |> Async.AwaitTask |> Async.Ignore

    let multiReact (msg: SocketUserMessage) (emotes: IEmote seq) = 
        emotes
        |> Seq.map (fun e -> fun _ -> react msg e)
        |> Async.Sequential
