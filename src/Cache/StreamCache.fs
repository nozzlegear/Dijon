namespace Dijon.Cache

open Dijon
open LazyCache

type PopulateStreamerRolesFunc = unit -> Async<int64 list>

type StreamCache() =
    let cache : IAppCache = upcast CachingService()
    let allRolesKey = "AllStreamerRoles"
    let mutable hasPopulated = false

    let getAllRoles () : Async<Set<int64>> =
        match cache.TryGetValue(allRolesKey) with
        | true, allRoles -> 
            async { return downcast allRoles }
        | false, _ -> 
            async { return Set.empty<int64> }

    /// Resets the cache, prompting the next <see cref="GetAllStreamerRoles" /> call to repopulate it.
    member _.Reset () =
        cache.Add(allRolesKey, Set.empty<int64>)
        hasPopulated <- false

    /// Adds the streamer role to the cache.
    member _.AddStreamerRole (roleId : int64) =
        async {
            let! allRoles = getAllRoles ()
            cache.Add(allRolesKey, Set.add roleId allRoles)
        }

    /// Removes the streamer role from the cache.
    member _.RemoveStreamerRole (roleId : int64) =
        async {
            let! allRoles = getAllRoles ()
            cache.Add(allRolesKey, Set.remove roleId allRoles)
        }

    member _.GetAllStreamerRoles (populate : PopulateStreamerRolesFunc) =
        let populate () =
            async {
                let! result = populate ()
                return Set.ofList result
            }

        if not hasPopulated then
            async {
                let! allRoles = populate ()

                cache.Add(allRolesKey, allRoles)
                hasPopulated <- true

                return allRoles
            }
        else
            cache.GetOrAddAsync<Set<int64>>(allRolesKey, populate >> Async.StartAsTask)
            |> Async.AwaitTask
