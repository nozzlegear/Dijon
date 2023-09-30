namespace Dijon.Database.LogChannels

open Dijon.Database
open Dijon.Shared

type ILogChannelsDatabase =
    abstract member GetLogChannelForGuild: guildId: GuildId -> Async<int64 option>
    abstract member SetLogChannelForGuild: guildId: GuildId -> channelId: int64 -> Async<unit>
    abstract member UnsetLogChannelForGuild: guildId: GuildId -> Async<unit>

type LogChannelsDatabase(
        dapperHelpers: IDapperHelpers
    ) =

    interface ILogChannelsDatabase with
        member x.GetLogChannelForGuild guildId =
            let sql = """
                SELECT ChannelId FROM DIJON_LOG_CHANNELS WHERE GuildId = @guildId
            """
            let data = dict [ "guildId" => match guildId with GuildId g -> g ]

            dapperHelpers.QuerySingleOrNone<int64>(sql, data)
            |> Async.AwaitTask

        member x.SetLogChannelForGuild guildId channelId =
            let sql = """
                MERGE DIJON_LOG_CHANNELS as Target
                USING (
                    SELECT @guildId
                ) AS Source (
                    GuildId
                ) ON (Target.GuildId = Source.GuildId)
                WHEN MATCHED THEN
                    UPDATE
                    SET ChannelId = @channelId
                WHEN NOT MATCHED THEN
                    Insert (
                        GuildId,
                        ChannelId
                    ) VALUES (
                        @guildId,
                        @channelId
                    )
                ;
            """
            let data = dict [
                "guildId" => match guildId with GuildId g -> g
                "channelId" => channelId
            ]

            dapperHelpers.Execute(sql, data)
            |> dapperHelpers.IgnoreResult
            |> Async.AwaitTask

        member x.UnsetLogChannelForGuild guildId =
            let sql = """
                DELETE FROM DIJON_LOG_CHANNELS WHERE GuildId = @guildId
            """
            let data = dict [ "guildId" => match guildId with GuildId g -> g ]

            dapperHelpers.Execute(sql, data)
            |> dapperHelpers.IgnoreResult
            |> Async.AwaitTask
    end
