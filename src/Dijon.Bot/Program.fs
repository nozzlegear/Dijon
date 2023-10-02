namespace Dijon

open Dijon.Database.Extensions
open Dijon.Bot.Extensions
open Dijon.Bot.Services
open Dijon.Shared

open Microsoft.Extensions.Configuration;
open Microsoft.Extensions.Logging;
open Microsoft.Extensions.Hosting
open System.Threading.Tasks

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
                    let configuration = context.Configuration

                    services.AddBotClient(configuration.GetRequiredSection "Discord")
                    services.AddDatabases(configuration.GetRequiredSection "Database")
                    services.AddBotServices())
                .ConfigureLogging(fun context logging ->
                    logging.AddConsole() |> ignore)
                .UseConsoleLifetime()
                .Build()

        host.RunAsync()
        |> Task.runSynchronously

        0 // return an integer exit code
