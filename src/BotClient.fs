namespace Dijon

open System
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

type BotClient(logger : ILogger<BotClient>, config : IConfiguration) =
    let token =
        match config.GetValue<string>("DIJON_BOT_TOKEN") with
        | ""
        | null -> failwith "DIJON_BOT_TOKEN was null or empty."
        | token -> token 

    let client = new DiscordSocketClient()
    
    do 
        let connectTask = 
            async {
                do! client.LoginAsync(TokenType.Bot, token) |> Async.AwaitTask
                do! client.StartAsync() |> Async.AwaitTask
                do! client.SetGameAsync "This Is Legal But We Question The Ethics" |> Async.AwaitTask
            }
            
        Async.RunSynchronously connectTask

    interface IAsyncDisposable with
        member x.DisposeAsync () =
            logger.LogWarning("The bot is disposing")
           
            client.StopAsync()
            |> ValueTask
    
    member x.UpdateGameAsync message =
        client.SetGameAsync(message)
