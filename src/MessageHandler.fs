namespace Dijon 
open Discord
open System
open Discord.WebSocket

type MessageHandler(database: IDijonDatabase, client: DiscordSocketClient) = 
    let startsWith (a: string) (b: string) = a.StartsWith(b, StringComparison.OrdinalIgnoreCase)
    let contains (a: string) (b: string) = a.Contains(b, StringComparison.OrdinalIgnoreCase)
    let lower (a: string) = a.ToLower()
    let trim (a: string) = a.Trim()
    let stripFirstWord (a: string) = a.Substring (a.IndexOf " " + 1) 

    let (|ContainsSlander|_|) (a: string) = 
        if contains a "#downwithdjur" || contains a "down with djur" || contains a ":downwithdjur:" 
        then Some ContainsSlander 
        else None

    let (|Mentioned|NotMentioned|) (msg: IMessage) = 
        let mentionString = sprintf "<@%i> " client.CurrentUser.Id

        if startsWith msg.Content "!dijon " || startsWith msg.Content mentionString then Mentioned
        else NotMentioned        

    let (|Ignore|Test|Verify|Status|SetLogChannel|Slander|BadCommand|) (msg: IMessage) = 
        match msg with 
        | NotMentioned -> 
            match msg.Content with 
            | ContainsSlander -> Slander 
            | _ -> Ignore
        | Mentioned ->
            match stripFirstWord msg.Content |> lower |> trim with 
            | "test" -> Test
            | "verify" -> Verify
            | "status" -> Status
            | "set log channel" -> SetLogChannel
            | ContainsSlander -> Slander 
            | _ -> BadCommand

    let handleTestMessage  (msg: IMessage) = async {
        printfn "Handling test message" 
    }

    let handleVerifyMessage (msg: IMessage) = async {
        printfn "Handling verify message"
    }

    let handleStatusMessage (msg: IMessage) = 
        let embed = EmbedBuilder()
        embed.Title <- ":robot: Dijon Status"
        embed.Description <- sprintf ":heartbeat: **%i ms** heartbeat latency." client.Latency
        embed.Color <- Nullable Color.Green

        msg.Channel.SendMessageAsync("", false, embed.Build())
        |> Async.AwaitTask
        |> Async.Ignore

    let handleSetLogChannelMessage (msg: IMessage) = async {
        printfn "Handling set log channel message"
    }

    let handleBadCommandMessage (msg: IMessage) = Async.Empty

    let handleSlander (msg: IMessage) = 
        // TODO: silence all dissidents
        printfn "Handling down with djur message"
        match msg with 
        | Mentioned -> 
            msg.Channel.SendMessageAsync "Don't talk shit." 
            |> Async.AwaitTask 
            |> Async.Ignore
        | NotMentioned -> Async.Empty 

    interface IMessageHandler with 
        member x.HandleMessage msg = 
            match msg with 
            | Ignore -> Async.Empty
            | Test -> handleTestMessage msg 
            | Verify -> handleVerifyMessage msg 
            | Status -> handleStatusMessage msg 
            | SetLogChannel -> handleSetLogChannelMessage msg 
            | BadCommand -> handleBadCommandMessage msg
            | Slander -> handleSlander msg 
