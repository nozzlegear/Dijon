namespace Dijon.Database.AffixChannels

open Dijon.Shared
open Dijon.Database

open DustyTables
open Microsoft.Extensions.Options
open System

type IAffixChannelsDatabase =
    abstract member ListAllAffixChannels: unit -> Async<AffixChannel list>
    abstract member GetAffixChannelForGuild: GuildId -> Async<AffixChannel option>
    abstract member SetAffixesChannelForGuild: guildId: GuildId -> channelId: int64 -> Async<unit>
    abstract member SetLastAffixesPostedForGuild: guildId: GuildId -> lastAffixesTitle: string -> Async<unit>

type AffixChannelsDatabase(options: IOptions<ConnectionStrings>) =
    let connectionString = options.Value.DefaultConnection

    let mapReaderToAffixChannels (reader: RowReader) : AffixChannel =
        let lastAffixes =
            match reader.stringOrNone "LastAffixesPosted" with
            | Some x when String.IsNullOrWhiteSpace x ->
                None
            | x ->
                x

        { GuildId = reader.int64 "GuildId"
          ChannelId = reader.int64 "ChannelId"
          LastAffixesPosted = lastAffixes }

    interface IAffixChannelsDatabase with
        member x.GetAffixChannelForGuild guildId =
            let sql = """
                SELECT * FROM DIJON_AFFIXES_CHANNELS WHERE GuildId = @guildId
            """

            Sql.connect connectionString
            |> Sql.query sql
            |> Sql.parameters [
                "guildId",  match guildId with GuildId g -> Sql.int64 g
            ]
            |> Sql.executeAsync mapReaderToAffixChannels
            |> Async.AwaitTask
            |> Async.Map Seq.tryHead

        member x.ListAllAffixChannels () =
            let sql = """
                SELECT * FROM DIJON_AFFIXES_CHANNELS
            """

            Sql.connect connectionString
            |> Sql.query sql
            |> Sql.executeAsync mapReaderToAffixChannels
            |> Async.AwaitTask

        member x.SetAffixesChannelForGuild guildId channelId =
            let sql = """
                MERGE DIJON_AFFIXES_CHANNELS as Target
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

            async {
                let job =
                    Sql.connect connectionString
                    |> Sql.query sql
                    |> Sql.parameters [
                        "guildId", match guildId with GuildId g -> Sql.int64 g
                        "channelId", Sql.int64 channelId
                    ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                let! _ = job
                ()
            }

        member x.SetLastAffixesPostedForGuild guildId lastAffixes =
            let sql = """
                UPDATE DIJON_AFFIXES_CHANNELS
                SET [LastAffixesPosted] = @lastAffixesPosted
                WHERE GuildId = @guildId
            """

            async {
                let job =
                    Sql.connect connectionString
                    |> Sql.query sql
                    |> Sql.parameters [
                        "guildId", match guildId with GuildId g -> Sql.int64 g
                        "lastAffixesPosted", Sql.string lastAffixes
                    ]
                    |> Sql.executeNonQueryAsync
                    |> Async.AwaitTask
                let! _ = job
                ()
            }
    end
