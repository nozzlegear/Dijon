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
                    services.AddSingleton<IDijonDatabase, DijonSqlDatabase>() |> ignore
                    services.AddSingleton<BotClient>() |> ignore 
                    services.AddHostedService<DatabaseMigratorService>() |> ignore
                    services.AddHostedService<StreamCheckService>() |> ignore
                    services.AddHostedService<AffixCheckService>() |> ignore
                    services.AddHostedService<UserMonitorService>() |> ignore
                    services.AddHostedService<HelpService>() |> ignore
                    services.AddHostedService<MemeService>() |> ignore
                    services.AddHostedService<StatusChangeService>() |> ignore)
                .ConfigureLogging(fun context logging ->
                    logging.AddConsole() |> ignore)
                .UseConsoleLifetime()
                .Build()

        // The bot client must be initialized to log the bot in
        host.Services.GetRequiredService<BotClient>().InitAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
                
        //host.RunConsoleAsync()
        host.RunAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously

        0 // return an integer exit code
