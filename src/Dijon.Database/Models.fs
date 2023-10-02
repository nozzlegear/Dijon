namespace Dijon.Database

open System.ComponentModel.DataAnnotations
open System.Collections.Generic

type DiscordId = DiscordId of int64
    with member x.AsInt64 = match x with DiscordId value -> value
type GuildId = GuildId of int64
    with member x.AsInt64 = match x with GuildId value -> value
type SqlParams = IDictionary<string, obj>

type ConnectionStrings = {
    [<Required>]
    DefaultConnection: string
}

