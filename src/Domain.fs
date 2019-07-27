namespace Dijon
open Discord
open Discord.WebSocket

type DiscordId = DiscordId of int64
type GuildId = GuildId of int64

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

type IDijonDatabase = 
    abstract member ListAsync: GuildId -> Async<Member list>
    abstract member BatchSetAsync: MemberUpdate seq -> Async<unit>
    abstract member UpdateAsync: MemberUpdate -> Async<unit>
    abstract member ConfigureAsync: unit -> Async<unit>

type IMessageHandler = 
    abstract member HandleMessage: IMessage -> Async<unit>

type BotConfig = 
    {
        database: IDijonDatabase
        client: DiscordSocketClient
        messages: IMessageHandler
    }