namespace Dijon
open Discord
open Discord.WebSocket
open System.Collections.Generic

type DiscordId = DiscordId of int64
type GuildId = GuildId of int64
type SqlParams = IDictionary<string, obj>

type Command = 
    | Ignore 
    | Test
    | Goulash 
    | Status 
    | SetLogChannel 
    | Slander 
    | Hype
    | Help
    | Unknown
    | AidAgainstSlander
    | FoxyLocation
    
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
    with 
    static member FromGuildUser (user: Discord.IGuildUser): MemberUpdate = 
        {
            DiscordId = int64 user.Id
            GuildId = int64 user.GuildId
            Username = user.Username
            Discriminator = user.Discriminator
            Nickname = user.Nickname
        }

type UniqueUser = 
    UniqueUser of DiscordId * GuildId
    with 
    static member FromMember (m: Member) = UniqueUser (DiscordId m.DiscordId, GuildId m.GuildId)
    static member FromMemberUpdate (m: MemberUpdate) = UniqueUser (DiscordId m.DiscordId, GuildId m.GuildId)    
    static member FromSocketGuildUser (m: SocketGuildUser) = UniqueUser (DiscordId <| int64 m.Id, GuildId <| int64 m.Guild.Id) 

type IDijonDatabase = 
    abstract member ListAsync: GuildId -> Async<Member list>
    abstract member BatchSetAsync: MemberUpdate seq -> Async<unit>
    abstract member DeleteAsync: UniqueUser -> Async<unit>
    abstract member GetLogChannelForGuild: GuildId -> Async<int64 option>
    abstract member SetLogChannelForGuild: GuildId -> int64 -> Async<unit>
    abstract member UnsetLogChannelForGuild: GuildId -> Async<unit>
    abstract member ConfigureAsync: unit -> Async<unit>

type IMessageHandler = 
    abstract member HandleMessage: IMessage -> Async<unit>
    abstract member SendUserLeftMessage: IMessageChannel -> GuildUser -> Async<unit> 

type BotConfig = 
    {
        database: IDijonDatabase
        client: DiscordSocketClient
        messages: IMessageHandler
    }
