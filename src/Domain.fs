namespace Dijon
open Dijon
open Discord
open Discord.WebSocket
open System.Collections.Generic
open Microsoft.Extensions.Configuration

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
    | GetAffix
    | SetAffixesChannel

module RaiderIo =
    open Thoth.Json.Net
    type Affix =
        {
            id: int
            name: string
            description: string
            wowhead_url: string
        }
        with
        static member Decoder : Decoder<Affix> =
            Decode.object (fun get ->
                { id = get.Required.Field "id" Decode.int
                  name = get.Required.Field "name" Decode.string
                  description = get.Required.Field "description" Decode.string
                  wowhead_url = get.Required.Field "wowhead_url" Decode.string } )
            
    type ListAffixesResponse =
        {
            region: string
            title: string
            leaderboard_url: string
            affix_details: Affix list 
        }
        with
        static member Decoder : Decoder<ListAffixesResponse> =
            Decode.object (fun get ->
                { region = get.Required.Field "region" Decode.string
                  title = get.Required.Field "title" Decode.string
                  leaderboard_url = get.Required.Field "leaderboard_url" Decode.string
                  affix_details = get.Required.Field "affix_details" (Decode.list Affix.Decoder)})
    
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

type AffixChannel =
    {
        GuildId: int64
        ChannelId: int64
        /// The title of the last affix message posted to this channel.
        LastAffixesPosted: string option
    }

type UniqueUser = 
    UniqueUser of DiscordId * GuildId
    with 
    static member FromMember (m: Member) = UniqueUser (DiscordId m.DiscordId, GuildId m.GuildId)
    static member FromMemberUpdate (m: MemberUpdate) = UniqueUser (DiscordId m.DiscordId, GuildId m.GuildId)    
    static member FromSocketGuildUser (m: SocketGuildUser) = UniqueUser (DiscordId <| int64 m.Id, GuildId <| int64 m.Guild.Id) 

type IDijonDatabase = 
    abstract member ListAsync: GuildId -> Async<Member list>
    abstract member ListAllAffixChannels: unit -> Async<AffixChannel list>
    abstract member BatchSetAsync: MemberUpdate seq -> Async<unit>
    abstract member DeleteAsync: UniqueUser -> Async<unit>
    abstract member GetLogChannelForGuild: GuildId -> Async<int64 option>
    abstract member GetAffixChannelForGuild: GuildId -> Async<AffixChannel option>
    abstract member SetLogChannelForGuild: GuildId -> int64 -> Async<unit>
    abstract member SetAffixesChannelForGuild: guildId: GuildId -> channelId: int64 -> Async<unit>
    abstract member SetLastAffixesPostedForGuild: guildId: GuildId -> lastAffixesTitle: string -> Async<unit>
    abstract member UnsetLogChannelForGuild: GuildId -> Async<unit>

type IMessageHandler = 
    abstract member HandleMessage: IMessage -> Async<unit>
    abstract member SendUserLeftMessage: IMessageChannel -> GuildUser -> Async<unit>
    abstract member SendAffixesMessage: IMessageChannel -> RaiderIo.ListAffixesResponse -> Async<unit>

type BotConfig = 
    {
        database: IDijonDatabase
        client: DiscordSocketClient
        messages: IMessageHandler
    }

type DatabaseOptions (config : IConfiguration) =
    member x.SqlConnectionString : string =
        let key = "DIJON_SQL_CONNECTION_STRING"
        match config.GetValue<string> key with
        | ""
        | null -> failwithf "%s was null or empty." key
        | token -> token 