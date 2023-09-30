namespace Dijon.Database

open System.ComponentModel.DataAnnotations
open System.Collections.Generic

type DiscordId = DiscordId of int64
type GuildId = GuildId of int64
type SqlParams = IDictionary<string, obj>

type ConnectionStrings = {
    [<Required>]
    DefaultConnection: string
}

