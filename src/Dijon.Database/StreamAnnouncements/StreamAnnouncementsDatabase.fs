namespace Dijon.Database.StreamAnnouncements

open Dijon.Database
open Dijon.Shared

open Npgsql.FSharp
open Microsoft.Extensions.Options
open System.Threading.Tasks

type IStreamAnnouncementsDatabase =
    abstract member AddStreamAnnouncementMessage: PartialStreamAnnouncementMessage -> Task<unit>
    abstract member DeleteStreamAnnouncementChannelForGuild: guildId: GuildId -> Task<unit>
    abstract member DeleteStreamAnnouncementMessageForStreamer: streamerId: int64 -> Task<unit>
    abstract member GetStreamAnnouncementChannelForGuild: guildId: GuildId -> Task<StreamAnnouncementChannel option>
    abstract member ListStreamAnnouncementChannels: unit -> Task<StreamAnnouncementChannel list>
    abstract member ListStreamAnnouncementMessagesForGuild: guildId: int64 -> Task<StreamAnnouncementMessage list>
    abstract member ListStreamAnnouncementMessagesForStreamer: streamerId: int64 -> Task<StreamAnnouncementMessage list>
    abstract member ListStreamerRoles: unit -> Task<Set<int64>>
    abstract member SetStreamAnnouncementChannelForGuild: PartialStreamAnnouncementChannel -> Task<unit>

type StreamAnnouncementsDatabase(options: IOptions<ConnectionStrings>) =
    let connectionString = options.Value.DefaultConnection

    let mapStreamAnnouncementMessages (read: RowReader) : StreamAnnouncementMessage =
        { Id = read.int "id"
          DateCreated = read.datetimeOffset "date_created"
          GuildId = read.int64 "guild_id"
          ChannelId = read.int64 "channel_id"
          MessageId = read.int64 "message_id"
          StreamerId = read.int64 "streamer_id" }

    let mapStreamAnnouncementChannels (read: RowReader) : StreamAnnouncementChannel =
        { Id = read.int "id"
          GuildId = read.int64 "guild_id"
          ChannelId = read.int64 "channel_id"
          StreamerRoleId = read.int64 "streamer_role_id" }

    interface IStreamAnnouncementsDatabase with
        member _.SetStreamAnnouncementChannelForGuild channel =
            connectionString
            |> Sql.connect
            |> Sql.query """
                INSERT INTO dijon_stream_announcement_channels (guild_id, channel_id, streamer_role_id)
                VALUES (@guildId, @channelId, @streamerRoleId)
                ON CONFLICT (guild_id) DO UPDATE
                    SET channel_id = @channelId,
                        streamer_role_id = @streamerRoleId
            """
            |> Sql.parameters
                [ "guildId", Sql.int64 channel.GuildId
                  "channelId", Sql.int64 channel.ChannelId
                  "streamerRoleId", Sql.int64 channel.StreamerRoleId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.GetStreamAnnouncementChannelForGuild guildId =
            connectionString
            |> Sql.connect
            |> Sql.query "SELECT * FROM dijon_stream_announcement_channels WHERE guild_id = @guildId"
            |> Sql.parameters [ "guildId", Sql.int64 guildId.AsInt64 ]
            |> Sql.executeAsync mapStreamAnnouncementChannels
            |> Task.map List.tryHead

        member _.DeleteStreamAnnouncementChannelForGuild guildId =
            connectionString
            |> Sql.connect
            |> Sql.query "DELETE FROM dijon_stream_announcement_channels WHERE guild_id = @guildId"
            |> Sql.parameters [ "guildId", Sql.int64 guildId.AsInt64 ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.ListStreamAnnouncementChannels () =
            connectionString
            |> Sql.connect
            |> Sql.query "SELECT * FROM dijon_stream_announcement_channels"
            |> Sql.executeAsync mapStreamAnnouncementChannels

        member self.ListStreamerRoles () =
            let self : IStreamAnnouncementsDatabase = upcast self

            task {
                let! channels = self.ListStreamAnnouncementChannels ()
                return channels
                       |> List.map (fun channel -> channel.StreamerRoleId)
                       |> Set
            }

        member _.AddStreamAnnouncementMessage message =
            connectionString
            |> Sql.connect
            |> Sql.query """
                INSERT INTO dijon_stream_announcement_messages (guild_id, channel_id, message_id, streamer_id)
                VALUES (@guildId, @channelId, @messageId, @streamerId)
            """
            |> Sql.parameters
                [ "guildId", Sql.int64 message.GuildId
                  "channelId", Sql.int64 message.ChannelId
                  "messageId", Sql.int64 message.MessageId
                  "streamerId", Sql.int64 message.StreamerId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.ListStreamAnnouncementMessagesForStreamer streamerId =
            connectionString
            |> Sql.connect
            |> Sql.query "SELECT * FROM dijon_stream_announcement_messages WHERE streamer_id = @streamerId"
            |> Sql.parameters [ "streamerId", Sql.int64 streamerId ]
            |> Sql.executeAsync mapStreamAnnouncementMessages

        member _.ListStreamAnnouncementMessagesForGuild guildId =
            connectionString
            |> Sql.connect
            |> Sql.query "SELECT * FROM dijon_stream_announcement_messages WHERE guild_id = @guildId"
            |> Sql.parameters [ "guildId", Sql.int64 guildId ]
            |> Sql.executeAsync mapStreamAnnouncementMessages

        member _.DeleteStreamAnnouncementMessageForStreamer streamerId =
            connectionString
            |> Sql.connect
            |> Sql.query "DELETE FROM dijon_stream_announcement_messages WHERE streamer_id = @streamerId"
            |> Sql.parameters [ "streamerId", Sql.int64 streamerId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore
    end
