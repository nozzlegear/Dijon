namespace Dijon.Bot

open Dijon.Bot

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions

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
                |> Async.AwaitTask
                |> Async.RunSynchronously
            ) |> ignore
