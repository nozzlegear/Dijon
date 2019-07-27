namespace Dijon

open System
open Discord

module Program = 
    let initDatabase () = 
        let database = 
            Environment.GetEnvironmentVariable "DIJON_SQL_CONNECTION_STRING"
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
        Environment.GetEnvironmentVariable "DIJON_BOT_TOKEN"
        |> Bot.Connect 

    [<EntryPoint>]
    let main argv =
        async {
            let! database = initDatabase()
            let! bot = initBot()
            let config = 
                {
                    database = database
                    client = bot 
                    messages = MessageHandler(database, bot) 
                }

            do! Bot.RecordAllUsers config 
            do! Bot.WireEventListeners config 
        } |> Async.RunSynchronously

        // Keep the program running until canceled    
        System.Threading.Tasks.Task.Delay -1
        |> Async.AwaitTask
        |> Async.RunSynchronously

        0 // return an integer exit code
