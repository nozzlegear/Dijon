namespace Dijon

open Dijon.Services
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

module Program = 
    [<EntryPoint>]
    let main _ =
        let host =
            Host.CreateDefaultBuilder()
                .ConfigureServices(fun context services ->
                    services.AddSingleton<DatabaseOptions>() |> ignore
                    services.AddSingleton<IDijonDatabase, DijonSqlDatabase> |> ignore
                    services.AddSingleton<BotClient>() |> ignore 
                    services.AddHostedService<DatabaseMigratorService>() |> ignore
                    services.AddHostedService<StreamCheckService>() |> ignore
                    services.AddHostedService<AffixCheckService>() |> ignore)
                .ConfigureLogging(fun context logging ->
                    logging.AddConsole() |> ignore)
                .RunConsoleAsync()
                
        Async.AwaitTask host
        |> Async.RunSynchronously

        0 // return an integer exit code
