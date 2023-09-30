namespace Dijon.Database

open Dijon.Database.AffixChannels
open Dijon.Database.GuildMembers
open Dijon.Database.LogChannels
open Dijon.Database.MessageReactionGuards
open Dijon.Database.StreamAnnouncements

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions

module Extensions =
    type IServiceCollection with
        member services.AddDatabases(configuration: IConfigurationSection) =
            services.AddOptions<ConnectionStrings>()
                .Bind(configuration)
                .ValidateDataAnnotations()
                .ValidateOnStart() |> ignore

            services.TryAddSingleton<IDapperHelpers, DapperHelpers>()
            services.TryAddSingleton<IStreamAnnouncementsDatabase, StreamAnnouncementsDatabase>()
            services.TryAddSingleton<ILogChannelsDatabase, LogChannelsDatabase>()
            services.TryAddSingleton<IGuildMembersDatabase, GuildMembersDatabase>()
            services.TryAddSingleton<IAffixChannelsDatabase, AffixChannelsDatabase>()
            services.TryAddSingleton<IMessageReactionGuardDatabase, MessageReactionGuardDatabase>()
