namespace Dijon.Database.StreamAnnouncements

open Dijon.Database
open Dijon.Shared

open DustyTables
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

    let mapStreamAnnouncementMessages (read : RowReader) : StreamAnnouncementMessage =
        { Id = read.int "Id"
          DateCreated = read.dateTimeOffset "DateCreated"
          GuildId = read.int64 "GuildId"
          ChannelId = read.int64 "ChannelId"
          MessageId = read.int64 "MessageId"
          StreamerId = read.int64 "StreamerId" }

    let mapStreamAnnouncementChannels (read : RowReader) : StreamAnnouncementChannel =
        { Id = read.int "Id"
          GuildId = read.int64 "GuildId"
          ChannelId = read.int64 "ChannelId"
          StreamerRoleId = read.int64 "StreamerRoleId" }

    interface IStreamAnnouncementsDatabase with
        member _.SetStreamAnnouncementChannelForGuild channel =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_SetStreamAnnouncementChannelForGuild"
            |> Sql.parameters
                [ "@guildId", Sql.int64 channel.GuildId
                  "@channelId", Sql.int64 channel.ChannelId
                  "@streamerRoleId", Sql.int64 channel.StreamerRoleId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.GetStreamAnnouncementChannelForGuild guildId =
            let guildId = match guildId with GuildId g -> g
            let mapAndCache job =
                async {
                    let! result = job

                    match Seq.tryHead result with
                    | Some channel ->
                        //do! streamCache.AddStreamerRole channel.StreamerRoleId
                        return Some channel
                    | None ->
                        return None
                }

            Sql.connect connectionString
            |> Sql.storedProcedure "sp_GetStreamAnnouncementChannelForGuild"
            |> Sql.parameters [ "@guildId", Sql.int64 guildId ]
            |> Sql.executeAsync mapStreamAnnouncementChannels
            |> Task.map Seq.tryHead

        member _.DeleteStreamAnnouncementChannelForGuild guildId =
            let guildId = match guildId with GuildId g -> g

            Sql.connect connectionString
            |> Sql.storedProcedure "sp_UnsetStreamAnnouncementChannelForGuild"
            |> Sql.parameters [ "@guildId", Sql.int64 guildId.AsInt64 ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.ListStreamAnnouncementChannels () =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_ListStreamAnnouncementChannels"
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
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_AddStreamAnnouncementMessage"
            |> Sql.parameters
                [ "@guildId", Sql.int64 message.GuildId
                  "@channelId", Sql.int64 message.ChannelId
                  "@messageId", Sql.int64 message.MessageId
                  "@streamerId", Sql.int64 message.StreamerId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.ListStreamAnnouncementMessagesForStreamer streamerId =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_ListStreamAnnouncementMessagesForStreamer"
            |> Sql.parameters
                [ "@streamerId", Sql.int64 streamerId ]
            |> Sql.executeAsync mapStreamAnnouncementMessages

        member _.ListStreamAnnouncementMessagesForGuild guildId =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_ListStreamAnnouncementMessagesForGuild"
            |> Sql.parameters
                [ "@guildId", Sql.int64 guildId ]
            |> Sql.executeAsync mapStreamAnnouncementMessages

        member _.DeleteStreamAnnouncementMessageForStreamer streamerId =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_DeleteStreamAnnouncementMessageForStreamer"
            |> Sql.parameters
                [ "@streamerId", Sql.int64 streamerId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore
    end

