namespace Dijon.Database.StreamAnnouncements

open Dijon.Database
open DustyTables
open Microsoft.Extensions.Options

type IStreamAnnouncementsDatabase =
    abstract member AddStreamAnnouncementMessage: PartialStreamAnnouncementMessage -> Async<unit>
    abstract member DeleteStreamAnnouncementChannelForGuild: guildId: GuildId -> Async<unit>
    abstract member DeleteStreamAnnouncementMessageForStreamer: streamerId: int64 -> Async<unit>
    abstract member GetStreamAnnouncementChannelForGuild: guildId: GuildId -> Async<StreamAnnouncementChannel option>
    abstract member ListStreamAnnouncementChannels: unit -> Async<StreamAnnouncementChannel list>
    abstract member ListStreamAnnouncementMessagesForGuild: guildId: int64 -> Async<StreamAnnouncementMessage list>
    abstract member ListStreamAnnouncementMessagesForStreamer: streamerId: int64 -> Async<StreamAnnouncementMessage list>
    abstract member ListStreamerRoles: unit -> Async<Set<int64>>
    abstract member SetStreamAnnouncementChannelForGuild: PartialStreamAnnouncementChannel -> Async<unit>

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
            let job =
                Sql.connect connectionString
                |> Sql.storedProcedure "sp_SetStreamAnnouncementChannelForGuild"
                |> Sql.parameters
                    [ "@guildId", Sql.int64 channel.GuildId
                      "@channelId", Sql.int64 channel.ChannelId
                      "@streamerRoleId", Sql.int64 channel.StreamerRoleId ]
                |> Sql.executeNonQueryAsync
                |> Async.AwaitTask
                |> Async.Ignore

            async {
                do! job
                //do! streamCache.AddStreamerRole(channel.StreamerRoleId)
            }

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
            |> Async.AwaitTask
            |> mapAndCache

        member _.DeleteStreamAnnouncementChannelForGuild guildId =
            let guildId = match guildId with GuildId g -> g

            let job =
                Sql.connect connectionString
                |> Sql.storedProcedure "sp_UnsetStreamAnnouncementChannelForGuild"
                |> Sql.parameters [ "@guildId", Sql.int64 guildId ]
                |> Sql.executeNonQueryAsync
                |> Async.AwaitTask

            async {
                let! _ = job
                // TODO: if we knew the streamer role for this guild, we could remove it from the cache instead of resetting the cache entirely
                //streamCache.Reset ()
                ()
            }

        member _.ListStreamAnnouncementChannels () =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_ListStreamAnnouncementChannels"
            |> Sql.executeAsync mapStreamAnnouncementChannels
            |> Async.AwaitTask

        member self.ListStreamerRoles () =
            let populate () =
                let self : IStreamAnnouncementsDatabase = upcast self

                async {
                    let! channels = self.ListStreamAnnouncementChannels ()
                    return channels
                           |> List.map (fun channel -> channel.StreamerRoleId)
                }

            //streamCache.GetAllStreamerRoles populate
            async {
                return Set.empty
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
            |> Async.AwaitTask
            |> Async.Ignore

        member _.ListStreamAnnouncementMessagesForStreamer streamerId =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_ListStreamAnnouncementMessagesForStreamer"
            |> Sql.parameters
                [ "@streamerId", Sql.int64 streamerId ]
            |> Sql.executeAsync mapStreamAnnouncementMessages
            |> Async.AwaitTask

        member _.ListStreamAnnouncementMessagesForGuild guildId =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_ListStreamAnnouncementMessagesForGuild"
            |> Sql.parameters
                [ "@guildId", Sql.int64 guildId ]
            |> Sql.executeAsync mapStreamAnnouncementMessages
            |> Async.AwaitTask

        member _.DeleteStreamAnnouncementMessageForStreamer streamerId =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_DeleteStreamAnnouncementMessageForStreamer"
            |> Sql.parameters
                [ "@streamerId", Sql.int64 streamerId ]
            |> Sql.executeNonQueryAsync
            |> Async.AwaitTask
            |> Async.Ignore
    end

