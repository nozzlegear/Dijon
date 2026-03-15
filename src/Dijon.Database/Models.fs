namespace Dijon.Database

open System.ComponentModel.DataAnnotations

type DiscordId = DiscordId of int64
    with member x.AsInt64 = match x with DiscordId value -> value
type GuildId = GuildId of int64
    with member x.AsInt64 = match x with GuildId value -> value

[<CLIMutable>]
type ConnectionStrings = {
    [<Required>]
    DefaultConnection: string
}

