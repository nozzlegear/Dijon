namespace Dijon
open Discord
open Discord.WebSocket
open System

type Streams(bot : BotClient, database : IDijonDatabase) = 
    /// Tries to get the user's stream activity. Returns None if the user is not streaming.
    member _.TryGetStreamActivity (user : SocketUser): Option<StreamingGame> = 
        user.Activities
        |> Seq.tryPick (function
            | :? StreamingGame as stream -> Some stream 
            | _ -> None)

    /// Checks if the user's activities indicate they're streaming. 
    member x.IsStreaming(user : SocketUser): bool = 
        x.TryGetStreamActivity user
        |> Option.isSome

    member _.PostStreamingMessage (user : SocketUser) (stream : StreamingGame) : Async<unit> =
        let embed = EmbedBuilder()
        embed.Title <- sprintf "%s is streaming %s right now!" user.Mention stream.Name
        embed.Color <- Nullable Color.Green
        embed.Description <- sprintf "%s. Check out their stream at %s" stream.Details stream.Url
        embed.Url <- stream.Url

        // TODO: get the server's stream messages channel id
        let channelId = int64 856354026509434890L
        let channel = bot.GetChannel channelId

        MessageUtils.sendEmbed (channel :> IChannel :?> IMessageChannel) embed
