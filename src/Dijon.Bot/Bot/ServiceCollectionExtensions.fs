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
            // The bot client must be initialized to log the bot in, which its host service will do.
            services.AddHostedService<BotClientHost>() |> ignore
