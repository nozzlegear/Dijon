namespace Dijon

open System
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

type BotClient(logger : ILogger<BotClient>, config : IConfiguration, database: IDijonDatabase) =
    let token =
        match config.GetValue<string>("DIJON_BOT_TOKEN") with
        | ""
        | null -> failwith "DIJON_BOT_TOKEN was null or empty."
        | token -> token 

    let client = new DiscordSocketClient()
    let messageHandler : IMessageHandler = upcast MessageHandler(database, client)
    let readyEvent = new System.Threading.ManualResetEvent false
    
    do 
        let connectTask =
            async {
                do! client.LoginAsync(TokenType.Bot, token) |> Async.AwaitTask
                do! client.StartAsync() |> Async.AwaitTask
                do! client.SetGameAsync "This Is Legal But We Question The Ethics" |> Async.AwaitTask
                do! Bot.WireEventListeners (fun _ -> readyEvent.Set() |> ignore) { database = database; client = client; messages = messageHandler }
            }
            
        Async.RunSynchronously connectTask

    interface IAsyncDisposable with
        member x.DisposeAsync () =
            logger.LogWarning("The bot is disposing")
            readyEvent.Dispose()
            client.StopAsync()
            |> ValueTask
    
    member x.UpdateGameAsync message =
        client.SetGameAsync(message)

    member x.PostAffixesMessageAsync (_ : GuildId) (channelId : int64) (affixes: RaiderIo.ListAffixesResponse) =
        async {
            // Wait for the bot's ready event to fire. If the bot is not yet ready, the channel will be null
            readyEvent.WaitOne() |> ignore
            
            sprintf "Posting affixes to channel %i" channelId 
            |> logger.LogInformation
            
            let channel = client.GetChannel (uint64 channelId) 
            
            return! messageHandler.SendAffixesMessage (channel :> IChannel :?> IMessageChannel) affixes
        }