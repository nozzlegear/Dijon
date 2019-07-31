namespace Dijon

open Dapper
open System.Data.SqlClient
open System.Data
open System
open System.Collections.Generic

type DijonSqlDatabase (connStr: string) = 
    let memberTableName = "DIJON_MEMBER_RECORDS"
    let channelTableName = "DIJON_LOG_CHANNELS"
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
                    Nickname = reader.GetString nickIndex
                }
        ]

    interface IDijonDatabase with 
        member x.ListAsync guildId = 
            let sql = 
                sprintf """
                SELECT * FROM %s WHERE GuildId = @id
                """
            let data = dict ["id" => match guildId with GuildId i -> i]

            executeReader (sql memberTableName) data
            |> Async.Map mapReaderToUsers

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

            query (sql channelTableName) data 
            |> Async.Map Seq.tryHead

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

            execute (sql channelTableName) data
            |> Async.Ignore

        member x.UnsetLogChannelForGuild guildId = 
            let sql = 
                sprintf """
                DELETE FROM %s WHERE GuildId = @guildId
                """
            let data = dict [ "guildId" => match guildId with GuildId g -> g ]

            execute (sql channelTableName) data 
            |> Async.Ignore

        member x.ConfigureAsync () =
            let memberSql = 
                sprintf """
                IF NOT EXISTS (SELECT * FROM sys.tables
                WHERE name = N'%s' AND type = 'U')

                BEGIN
                CREATE TABLE [dbo].[%s] (
                    Id int identity(1,1) primary key,
                    DiscordId bigint not null index idx_discordid,
                    GuildId bigint not null index idx_guildid,
                    FirstSeenAt datetime2 not null,
                    Username nvarchar(500) not null,
                    Discriminator nvarchar (12) not null,
                    Nickname nvarchar (1000) null
                )
                END
                """
            let channelSql = 
                sprintf """
                IF NOT EXISTS (SELECT * FROM sys.tables
                WHERE name = N'%s' AND type = 'U')

                BEGIN
                CREATE TABLE [dbo].[%s] (
                    Id int identity(1,1) primary key,
                    GuildId bigint not null index idx_guildid,
                    ChannelId bigint not null
                )
                END
                """
            [
                execute (memberSql memberTableName memberTableName) (Map.empty)
                execute (channelSql channelTableName channelTableName) (Map.empty)
            ]
            |> Async.Parallel
            |> Async.Ignore
        