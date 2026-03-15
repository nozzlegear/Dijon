namespace Dijon.Database.LogChannels

open Dijon.Database
open Dijon.Shared

open Npgsql.FSharp
open Microsoft.Extensions.Options
open System.Threading.Tasks

type ILogChannelsDatabase =
    abstract member GetLogChannelForGuild: guildId: GuildId -> Task<int64 option>
    abstract member SetLogChannelForGuild: guildId: GuildId -> channelId: int64 -> Task<unit>
    abstract member UnsetLogChannelForGuild: guildId: GuildId -> Task<unit>

type LogChannelsDatabase(options: IOptions<ConnectionStrings>) =
    let connectionString = options.Value.DefaultConnection

    interface ILogChannelsDatabase with
        member _.GetLogChannelForGuild guildId =
            connectionString
            |> Sql.connect
            |> Sql.query "SELECT channel_id FROM dijon_log_channels WHERE guild_id = @guildId"
            |> Sql.parameters [ "guildId", Sql.int64 guildId.AsInt64 ]
            |> Sql.executeAsync (fun r -> r.int64 "channel_id")
            |> Task.map List.tryHead

        member _.SetLogChannelForGuild guildId channelId =
            connectionString
            |> Sql.connect
            |> Sql.query """
                INSERT INTO dijon_log_channels (guild_id, channel_id)
                VALUES (@guildId, @channelId)
                ON CONFLICT (guild_id) DO UPDATE SET channel_id = @channelId
            """
            |> Sql.parameters [
                "guildId", Sql.int64 guildId.AsInt64
                "channelId", Sql.int64 channelId
            ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.UnsetLogChannelForGuild guildId =
            connectionString
            |> Sql.connect
            |> Sql.query "DELETE FROM dijon_log_channels WHERE guild_id = @guildId"
            |> Sql.parameters [ "guildId", Sql.int64 guildId.AsInt64 ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore
    end
