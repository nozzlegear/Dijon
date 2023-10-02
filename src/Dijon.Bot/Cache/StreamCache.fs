namespace Dijon.Bot.Cache

open Dijon.Database
open Dijon.Database.StreamAnnouncements

open LazyCache
open Microsoft.Extensions.Caching.Memory
open System
open System.Threading.Tasks

type IStreamCache =
    /// Formats a cache key for the guild's stream announcements channel.
    abstract member FormatStreamAnnouncementChannelKey: guildId: GuildId -> string
    /// Loads the guild's streamer role and stream announcement channel from memory. If it the data isn't found, the
    /// <see cref="IStreamAnnouncementsDatabase"/> will be called to attempt to load it from there.
    abstract member LoadStreamDataForGuild: guildId: GuildId -> Task<StreamAnnouncementChannel option>
    /// Releases the guild's stream data from memory.
    abstract member ReleaseStreamDataForGuild: guildId: GuildId -> unit

type StreamCache(
    cache: IAppCache,
    database: IStreamAnnouncementsDatabase
) =
    let [<Literal>] streamAnnouncementsChannelIdPrefix = "StreamAnnouncementsChannel:Guild"
    let OneHour = TimeSpan.FromHours 1

    // let getAllRoles () : Async<Set<int64>> =
    //     match cache.TryGetValue(allRolesKey) with
    //     | true, (:? AsyncLazy<Set<int64>> as allRoles) ->
    //         Async.AwaitTask allRoles.Value
    //     | true, (:? Lazy<Set<int64>> as allRoles) ->
    //         async { return allRoles.Value }
    //     | true, allRoles ->
    //         async { return downcast allRoles }
    //     | false, _ ->
    //         async { return Set.empty<int64> }

    interface IStreamCache with
        member _.FormatStreamAnnouncementChannelKey (guildId: GuildId) =
            $"{streamAnnouncementsChannelIdPrefix}:{guildId.AsInt64}"

        member this.LoadStreamDataForGuild (guildId: GuildId) =
            let this = this :> IStreamCache
            let key = this.FormatStreamAnnouncementChannelKey(guildId)

            cache.GetOrAddAsync<StreamAnnouncementChannel option>(key, Func<ICacheEntry, Task<StreamAnnouncementChannel option>>(fun (entry: ICacheEntry) ->
                // Cache the result for one hour, as stream role ids are unlikely to change often
                entry.AbsoluteExpirationRelativeToNow <- OneHour
                database.GetStreamAnnouncementChannelForGuild guildId
            ))

        member this.ReleaseStreamDataForGuild (guildId: GuildId) =
            let this = this :> IStreamCache
            let key = this.FormatStreamAnnouncementChannelKey(guildId)
            cache.Remove key
    end
