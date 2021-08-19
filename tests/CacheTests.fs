module CacheTests

open Xunit
open Dijon.Cache

[<Fact>]
let ``Adds roles to the cache`` () =
    let cache = StreamCache()
    let populate = fun _ -> async {
        return [ 123456L; 234567L; 345678L ]
    }

    async {
        let! result = cache.GetAllStreamerRoles(populate)

        Assert.Equal(3, Set.count result)
        cache.AddStreamerRole 456789L

        let! result = cache.GetAllStreamerRoles(populate)

        Assert.Equal(4, Set.count result)
        Assert.True(Set.contains 456789L result)
    }

[<Fact>]
let ``Populates roles when the cache is empty`` () = 
    let cache = StreamCache()
    let mutable populated = false
    let populate = fun _ -> async {
        populated <- true
        return [ 123456L; 234567L; 345678L; ]
    }

    async {
        let! _ = cache.GetAllStreamerRoles(populate)

        Assert.True(populated)

        populated <- false

        let! _ = cache.GetAllStreamerRoles(populate)

        Assert.False(populated)
    }

[<Fact>]
let ``Lists roles in the cache`` () =
    let cache = StreamCache()
    let mutable populated = false
    let populate = fun _ -> async {
        populated <- true
        return [ 123456L; 234567L; 345678L; ]
    }

    async {
        let! result = cache.GetAllStreamerRoles(populate)

        Assert.True(populated)
        Assert.Equal(3, Set.count result)
        Assert.True(Set.ofList [ 123456L; 234567L; 345678L ] = result)
    }

[<Fact>]
let ``Removes roles from the cache`` () =
    let cache = StreamCache()
    let populate = fun _ -> async {
        return [ 123456L; 234567L; 345678L; ]
    }

    async {
        let! result = cache.GetAllStreamerRoles(populate)
        
        Assert.Equal(3, Set.count result)
        cache.RemoveStreamerRole(345678L)

        let! result = cache.GetAllStreamerRoles(populate)

        Assert.Equal(2, Set.count result)
        Assert.False(Set.contains 345678L result)
    }

[<Fact>]
let ``Does not return roles added before the cache has been populated`` () =
    let cache = StreamCache()
    let populate = fun _ -> async {
        return List.empty
    }
    cache.AddStreamerRole(123456L)
    cache.AddStreamerRole(234567L)
    cache.AddStreamerRole(345678L)

    async {
        let! result = cache.GetAllStreamerRoles(populate)

        // Should not add roles before the first `GetAllStreamerRoles` is called
        Assert.Equal(0, Set.count result)
        cache.AddStreamerRole(123456L)

        let! result = cache.GetAllStreamerRoles(populate)
        
        Assert.Equal(1, Set.count result)
        Assert.True(Set.contains 123456L result)
    }
