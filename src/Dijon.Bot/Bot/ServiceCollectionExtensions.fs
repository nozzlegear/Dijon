namespace Dijon.Bot

open Dijon.Bot
open Dijon.Shared

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open System.Threading.Tasks

module Extensions =
    type IServiceCollection with
        member services.AddBotClient(configuration: IConfigurationSection) =
            services.AddOptions<BotClientOptions>()
                .Bind(configuration)
                .ValidateDataAnnotations()
                .ValidateOnStart() |> ignore

            services.TryAddSingleton<IBotClient, BotClient>()
            services.PostConfigure<IBotClient>(fun botClient ->
                // The bot client must be initialized to log the bot in
                botClient.InitAsync()
                |> Task.runSynchronously
            ) |> ignore
