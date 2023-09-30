namespace Dijon.Bot.Services

open Dijon.Bot.Cache
open Dijon.Bot.Services

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions

[<AutoOpen>]
module Extensions =
    type IServiceCollection with
        member services.AddBotServices() =
            services.TryAddSingleton<IStreamCache, StreamCache>()
            services.AddHostedService<DatabaseMigratorService>() |> ignore
            services.AddHostedService<StreamCheckService>() |> ignore
            services.AddHostedService<ReactionGuardService>() |> ignore
            services.AddHostedService<AffixCheckService>() |> ignore
            services.AddHostedService<UserMonitorService>() |> ignore
            services.AddHostedService<HelpService>() |> ignore
            services.AddHostedService<MemeService>() |> ignore
            services.AddHostedService<StatusChangeService>() |> ignore
