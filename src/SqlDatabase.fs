namespace Dijon

open Dapper
open System.Data.SqlClient
open System.Data
open System
open DustyTables

type DijonSqlDatabase (options : DatabaseOptions) =
    let connStr = options.SqlConnectionString
    let memberTableName = "DIJON_MEMBER_RECORDS"
    let logChannelTableName = "DIJON_LOG_CHANNELS"
    let affixesChannelTableName = "DIJON_AFFIXES_CHANNELS"
    let (=>) a b = a, box b
    let dispose (conn: IDisposable) = Async.Iter (fun _ -> conn.Dispose())
    let getDiscordId = function 
        | UniqueUser (discordId, _) -> match discordId with DiscordId i -> i
    let getGuildId = function 
        | UniqueUser (_, guildId) -> match guildId with GuildId i -> i    
    let querySingle sql (data: SqlParams) = async {
        use conn = new SqlConnection(connStr)
        return! conn.QuerySingleAsync<_>(sql, data) |> Async.AwaitTask
    }
    let query sql (data: SqlParams) = async {
        use conn = new SqlConnection(connStr)
        return! conn.QueryAsync<_>(sql, data) |> Async.AwaitTask
    }
    let execute sql (data: SqlParams) = async {
        use conn = new SqlConnection(connStr)
        return! conn.ExecuteAsync(sql, data) |> Async.AwaitTask
    }
    let executeReader sql (data: SqlParams) = async {
        use conn = new SqlConnection(connStr)
        return! conn.ExecuteReaderAsync(sql, data) |> Async.AwaitTask
    }

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

    let userExists user =
        let sql = 
            sprintf """
            SELECT CASE WHEN EXISTS (
                SELECT *
                FROM %s
                WHERE DiscordId = @discordId
                AND GuildId = @guildId
            )
            THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT) END
            """
        let data = dict [
            "discordId" => getDiscordId user 
            "guildId" => getGuildId user 
        ]
        querySingle (sql memberTableName) data

    let createUser (user: MemberUpdate) =
        let sql = 
            sprintf """
            INSERT INTO %s (
                DiscordId,
                GuildId,
                FirstSeenAt,
                UserName,
                Discriminator,
                Nickname
            ) VALUES (
                @discordId,
                @guildId,
                @firstSeenAt,
                @username,
                @discriminator,
                @nickname
            )
            """
        let data = dict [
            "discordId" => user.DiscordId
            "guildId" => user.GuildId
            "firstSeenAt" => DateTimeOffset.UtcNow
            "username" => user.Username
            "discriminator" => user.Discriminator
            "nickname" => user.Nickname
        ]
        execute (sql memberTableName) data
    
    let updateUser (updatedMember: MemberUpdate) = 
        let sql = 
            sprintf """
            UPDATE %s SET 
            UserName = @username,
            Discriminator = @disc,
            Nickname = @nick
            WHERE DiscordId = @id
            """
        let data = dict [
            "username" => updatedMember.Username
            "disc" => updatedMember.Discriminator
            "nick" => updatedMember.Nickname
            "id" => updatedMember.DiscordId
        ]

        executeReader (sql memberTableName) data
        |> Async.Ignore

    let mapReaderToUsers (reader: IDataReader): Member list = 
        // Get the index of each column that should be mapped to a property
        let idIndex = reader.GetOrdinal "Id"
        let discordIdIndex = reader.GetOrdinal "DiscordId"
        let guildIdIndex = reader.GetOrdinal "GuildId"
        let firstSeenIndex = reader.GetOrdinal "FirstSeenAt"
        let usernameIndex = reader.GetOrdinal "Username"
        let discIndex = reader.GetOrdinal "Discriminator"
        let nickIndex = reader.GetOrdinal "Nickname"

        // Loop through each row in the reader and map it to a Member
        [
            while reader.Read() do 
                yield {
                    Id = reader.GetInt32 idIndex 
                    DiscordId = reader.GetInt64 discordIdIndex
                    GuildId = reader.GetInt64 guildIdIndex 
                    FirstSeenAt = reader.GetDateTime firstSeenIndex |> DateTimeOffset
                    Username = reader.GetString usernameIndex
                    Discriminator = reader.GetString discIndex
                    Nickname = if reader.IsDBNull nickIndex then String.Empty else reader.GetString nickIndex
                }
        ]
        
    let mapReaderToAffixChannels (reader: IDataReader) : AffixChannel list =
        [
            while reader.Read() do
                let guildIdColumn = reader.GetOrdinal "GuildId"
                let channelIdColumn = reader.GetOrdinal "ChannelId"
                let lastAffixesColumn = reader.GetOrdinal "LastAffixesPosted"
                let guildId = reader.GetInt64 guildIdColumn
                let channelId = reader.GetInt64 channelIdColumn
                let lastAffixes =
                    match reader.GetValue lastAffixesColumn with
                    | :? DBNull ->
                        None
                    | :? String as x when String.IsNullOrWhiteSpace x ->
                        None
                    | :? String as x ->
                        Some x
                    | x ->
                        failwithf "Failed to read last affixes channel, value was type %s" (x.GetType().FullName)
                
                yield { GuildId = guildId
                        ChannelId = channelId
                        LastAffixesPosted = lastAffixes }
        ]

    interface IDijonDatabase with 
        member x.ListAsync guildId = 
            let sql = 
                sprintf """
                SELECT * FROM %s WHERE GuildId = @id
                """
            let data = dict ["id" => match guildId with GuildId i -> i]

            async {
                use conn = new SqlConnection(connStr)
                let! reader = conn.ExecuteReaderAsync(sql memberTableName, data) |> Async.AwaitTask
                let result = mapReaderToUsers reader
                conn.Dispose()
                return result
            }
            
        member x.ListAllAffixChannels () =
            let sql =
                sprintf """
                SELECT * FROM %s
                """

            async {
                use conn = new SqlConnection(connStr)
                let! reader = conn.ExecuteReaderAsync(sql affixesChannelTableName) |> Async.AwaitTask
                let result = mapReaderToAffixChannels reader
                conn.Dispose()
                return result
            }

        member x.BatchSetAsync members = 
            // Cheating for now until I can get a batch update in
            members
            |> Seq.map (fun m -> async {
                let! userExists = userExists (UniqueUser.FromMemberUpdate m)

                if not userExists then 
                    do! createUser m |> Async.Ignore 
                else 
                    do! updateUser m 
            })
            |> Async.Parallel
            |> Async.Ignore
        
        member x.DeleteAsync user = 
            let sql = 
                sprintf """
                DELETE FROM %s WHERE GuildId = @guildId AND Discordid = @discordId
                """
            let data = dict [
                "guildId" => getGuildId user 
                "discordId" => getDiscordId user 
            ]

            execute (sql memberTableName) data 
            |> Async.Ignore
        
        member x.GetLogChannelForGuild guildId = 
            let sql = 
                sprintf """
                SELECT ChannelId FROM %s WHERE GuildId = @guildId
                """
            let data = dict [ "guildId" => match guildId with GuildId g -> g ]

            query (sql logChannelTableName) data 
            |> Async.Map Seq.tryHead
            
        member x.GetAffixChannelForGuild guildId = 
            let sql = 
                sprintf """
                SELECT * FROM %s WHERE GuildId = @guildId
                """
            let data = dict [ "guildId" => match guildId with GuildId g -> g ]
            
            async {
                use conn = new SqlConnection(connStr)
                let! reader = conn.ExecuteReaderAsync(sql affixesChannelTableName, data) |> Async.AwaitTask
                let result = mapReaderToAffixChannels reader
                conn.Dispose()
                return result |> Seq.tryHead 
            }

        member x.SetLogChannelForGuild guildId channelId = 
            let sql = 
                sprintf """
                MERGE %s as Target
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

            execute (sql logChannelTableName) data
            |> Async.Ignore
            
        member x.SetAffixesChannelForGuild guildId channelId =
            let sql = 
                sprintf """
                MERGE %s as Target
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

            execute (sql affixesChannelTableName) data
            |> Async.Ignore
            
        member x.SetLastAffixesPostedForGuild guildId lastAffixes =
            let sql =
                sprintf """
                UPDATE %s SET [LastAffixesPosted] = @lastAffixesPosted WHERE GuildId = @guildId
                """
            let data = dict [
                "guildId" => match guildId with GuildId g -> g
                "lastAffixesPosted" => lastAffixes
            ]
            
            execute (sql affixesChannelTableName) data
            |> Async.Ignore 

        member x.UnsetLogChannelForGuild guildId = 
            let sql = 
                sprintf """
                DELETE FROM %s WHERE GuildId = @guildId
                """
            let data = dict [ "guildId" => match guildId with GuildId g -> g ]

            execute (sql logChannelTableName) data 
            |> Async.Ignore

        member _.SetStreamAnnouncementChannelForGuild channel =
            Sql.connect connStr
            |> Sql.storedProcedure "sp_SetStreamAnnouncementChannelForGuild"
            |> Sql.parameters 
                [ "@guildId", Sql.int64 channel.GuildId
                  "@channelId", Sql.int64 channel.ChannelId
                  "@streamerRoleId", Sql.int64 channel.StreamerRoleId ]
            |> Sql.executeNonQueryAsync
            |> Async.AwaitTask
            |> Async.Ignore

        member _.GetStreamAnnouncementChannelForGuild guildId =
            let toOption job = 
                async {
                    let! result = job
                    return Seq.tryHead result
                }

            Sql.connect connStr
            |> Sql.storedProcedure "sp_GetStreamAnnouncementChannelForGuild"
            |> Sql.parameters [ "@guildId", match guildId with GuildId g -> Sql.int64 g ]
            |> Sql.executeAsync mapStreamAnnouncementChannels
            |> Async.AwaitTask
            |> toOption

        member _.DeleteStreamAnnouncementChannelForGuild guildId =
            Sql.connect connStr
            |> Sql.storedProcedure "sp_UnsetStreamAnnouncementChannelForGuild"
            |> Sql.parameters [ "@guildId", match guildId with GuildId g -> Sql.int64 g ]
            |> Sql.executeNonQueryAsync
            |> Async.AwaitTask
            |> Async.Ignore

        member _.ListStreamAnnouncementChannels () =
            Sql.connect connStr
            |> Sql.storedProcedure "sp_ListStreamAnnouncementChannels"
            |> Sql.executeAsync mapStreamAnnouncementChannels
            |> Async.AwaitTask

        member _.AddStreamAnnouncementMessage message =
            Sql.connect connStr
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
            Sql.connect connStr
            |> Sql.storedProcedure "sp_ListStreamAnnouncementMessagesForStreamer"
            |> Sql.parameters
                [ "@streamerId", Sql.int64 streamerId ]
            |> Sql.executeAsync mapStreamAnnouncementMessages
            |> Async.AwaitTask

        member _.ListStreamAnnouncementMessagesForGuild guildId =
            Sql.connect connStr
            |> Sql.storedProcedure "sp_ListStreamAnnouncementMessagesForGuild"
            |> Sql.parameters
                [ "@guildId", Sql.int64 guildId ]
            |> Sql.executeAsync mapStreamAnnouncementMessages
            |> Async.AwaitTask

        member _.DeleteStreamAnnouncementMessageForStreamer streamerId =
            Sql.connect connStr
            |> Sql.storedProcedure "sp_DeleteStreamAnnouncementMessageForStreamer"
            |> Sql.parameters
                [ "@streamerId", Sql.int64 streamerId ]
            |> Sql.executeNonQueryAsync
            |> Async.AwaitTask
            |> Async.Ignore
