namespace Dijon.Database.AffixChannels

open System.Threading.Tasks
open Dijon.Shared
open Dijon.Database

open Npgsql.FSharp
open Microsoft.Extensions.Options
open System

type IAffixChannelsDatabase =
    abstract member ListAllAffixChannels: unit -> Task<AffixChannel list>
    abstract member GetAffixChannelForGuild: GuildId -> Task<AffixChannel option>
    abstract member RemoveAffixesChannelForGuild: guildId: GuildId -> Task<unit>
    abstract member SetAffixesChannelForGuild: guildId: GuildId -> channelId: int64 -> Task<unit>
    abstract member SetLastAffixesPostedForGuild: guildId: GuildId -> lastAffixesTitle: string -> Task<unit>

type AffixChannelsDatabase(options: IOptions<ConnectionStrings>) =
    let connectionString = options.Value.DefaultConnection

    let mapReaderToAffixChannels (reader: RowReader) : AffixChannel =
        let lastAffixes =
            match reader.stringOrNone "last_affixes_posted" with
            | Some x when String.IsNullOrWhiteSpace x ->
                None
            | x ->
                x

        { GuildId = reader.int64 "guild_id"
          ChannelId = reader.int64 "channel_id"
          LastAffixesPosted = lastAffixes }

    interface IAffixChannelsDatabase with
        member _.GetAffixChannelForGuild guildId =
            connectionString
            |> Sql.connect
            |> Sql.query "SELECT * FROM dijon_affix_channels WHERE guild_id = @guildId"
            |> Sql.parameters [ "guildId", Sql.int64 guildId.AsInt64 ]
            |> Sql.executeAsync mapReaderToAffixChannels
            |> Task.map List.tryHead

        member _.ListAllAffixChannels () =
            connectionString
            |> Sql.connect
            |> Sql.query "SELECT * FROM dijon_affix_channels"
            |> Sql.executeAsync mapReaderToAffixChannels

        member _.RemoveAffixesChannelForGuild guildId =
            connectionString
            |> Sql.connect
            |> Sql.query "DELETE FROM dijon_affix_channels WHERE guild_id = @guildId"
            |> Sql.parameters [ "guildId", Sql.int64 guildId.AsInt64 ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.SetAffixesChannelForGuild guildId channelId =
            connectionString
            |> Sql.connect
            |> Sql.query """
                INSERT INTO dijon_affix_channels (guild_id, channel_id)
                VALUES (@guildId, @channelId)
                ON CONFLICT (guild_id) DO UPDATE SET channel_id = @channelId
            """
            |> Sql.parameters [
                "guildId", Sql.int64 guildId.AsInt64
                "channelId", Sql.int64 channelId
            ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.SetLastAffixesPostedForGuild guildId lastAffixes =
            connectionString
            |> Sql.connect
            |> Sql.query """
                UPDATE dijon_affix_channels
                SET last_affixes_posted = @lastAffixesPosted
                WHERE guild_id = @guildId
            """
            |> Sql.parameters [
                "guildId", Sql.int64 guildId.AsInt64
                "lastAffixesPosted", Sql.string lastAffixes
            ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore
    end
