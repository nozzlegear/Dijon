namespace Dijon.Bot.Cache

open LazyCache
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions

[<AutoOpen>]
module Extensions =
    type IServiceCollection with
        member services.AddStreamCache() =
            services.TryAddSingleton<IAppCache, CachingService>()
            services.TryAddSingleton<IStreamCache, StreamCache>()
