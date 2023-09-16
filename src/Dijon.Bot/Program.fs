namespace Dijon

open Dijon.Cache
open Dijon.Services

open Microsoft.Extensions.Options;
open Microsoft.Extensions.Configuration;
open Microsoft.Extensions.DependencyInjection;
open Microsoft.Extensions.Logging;

open Microsoft.Extensions.Options
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

module Program =
    [<EntryPoint>]
    let main _ =
        let host =
            Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(fun builder ->
                    builder.AddJsonFile("appsettings.local.json", optional = true)
                        // Add Podman secret files to bindable configurations
                        .AddKeyPerFile("/run/secrets", optional = true) |> ignore)
                .ConfigureServices(fun context services ->

                    services.AddOptions<DatabaseOptions>()
                        .BindConfiguration("Database")
                        .ValidateDataAnnotations() |> ignore

                    services.AddOptions<BotClientOptions>()
                        .BindConfiguration("BotClient")
                        .ValidateDataAnnotations() |> ignore

                    services.AddSingleton<StreamCache>() |> ignore
                    services.AddSingleton<IDijonDatabase, DijonSqlDatabase>() |> ignore
                    services.AddSingleton<BotClient>() |> ignore
                    services.AddHostedService<DatabaseMigratorService>() |> ignore
                    services.AddHostedService<StreamCheckService>() |> ignore
                    services.AddHostedService<ReactionGuardService>() |> ignore
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
