namespace Dijon.Database.GuildMembers

open Dijon.Database

type GuildUser =
    {
        Nickname: string option
        UserName: string
        Discriminator: string
        AvatarUrl: string
    }

type Member =
    {
        Id: int
        DiscordId: int64
        GuildId: int64
        FirstSeenAt: System.DateTimeOffset
        Username: string
        Discriminator: string
        Nickname: string
    }

type MemberUpdate =
    {
        DiscordId: int64
        GuildId: int64
        Username: string
        Discriminator: string
        Nickname: string
    }
    //with
    //static member FromGuildUser (user: IGuildUser): MemberUpdate =
    //    {
    //        DiscordId = int64 user.Id
    //        GuildId = int64 user.GuildId
    //        Username = user.Username
    //        Discriminator = user.Discriminator
    //        Nickname = user.Nickname
    //    }

type UniqueUser =
    UniqueUser of DiscordId * GuildId
    with
    static member FromMember (m: Member) = UniqueUser (DiscordId m.DiscordId, GuildId m.GuildId)
    static member FromMemberUpdate (m: MemberUpdate) = UniqueUser (DiscordId m.DiscordId, GuildId m.GuildId)
    //static member FromSocketGuildUser (m: SocketGuildUser) = UniqueUser (DiscordId <| int64 m.Id, GuildId <| int64 m.Guild.Id)
