namespace Dijon

open System
open Dijon.Services
open Discord
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

module Program = 
    let requiredEnv key = 
        match Environment.GetEnvironmentVariable key with 
        | x when String.IsNullOrEmpty x -> failwithf "Required environment key %s was null or empty." key 
        | x -> x 

    let initDatabase () =
        let database = 
            requiredEnv "DIJON_SQL_CONNECTION_STRING"
            |> DijonSqlDatabase
            :> IDijonDatabase

        async {
            let! configureResult = database.ConfigureAsync() |> Async.Catch

            match configureResult with 
            | Choice1Of2 _ -> printfn "Configure database"
            | Choice2Of2 exn -> printfn "Error configuring database: %s" exn.Message

            return database
        }
    
    let initBot () =
        requiredEnv "DIJON_BOT_TOKEN"
        |> Bot.Connect 

    [<EntryPoint>]
    let main argv =
        let host =
            Host.CreateDefaultBuilder()
                .ConfigureServices(fun context services ->
                    services.AddSingleton<BotClient>() |> ignore 
//                    services.AddHostedService<BotService> |> ignore
                    services.AddHostedService<StreamCheckService>() |> ignore
                    services.AddHostedService<AffixCheckService>() |> ignore)
                .ConfigureLogging(fun context logging ->
                    logging.AddConsole() |> ignore)
                .RunConsoleAsync()
                
        Async.AwaitTask host
        |> Async.RunSynchronously
        
//        async {
//            let! database = initDatabase()
//            let! bot = initBot()
//            let config = 
//                {
//                    database = database
//                    client = bot 
//                    messages = MessageHandler(database, bot) 
//                }
//
//            do! Bot.RecordAllUsers config 
//            do! Bot.WireEventListeners config 
//        } |> Async.RunSynchronously
//
//        // Keep the program running until canceled    
//        System.Threading.Tasks.Task.Delay -1
//        |> Async.AwaitTask
//        |> Async.RunSynchronously

        0 // return an integer exit code
