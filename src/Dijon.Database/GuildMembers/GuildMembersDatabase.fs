namespace Dijon.Database.GuildMembers

open Dijon.Database
open Dijon.Shared

open System
open System.Data

type IGuildMembersDatabase =
    abstract member BatchSetAsync: MemberUpdate seq -> Async<unit>
    abstract member DeleteAsync: UniqueUser -> Async<unit>
    abstract member ListAsync: GuildId -> Async<Member list>

type GuildMembersDatabase(
        dapperHelpers: IDapperHelpers
    ) =

    let getDiscordId = function
        | UniqueUser (discordId, _) -> match discordId with DiscordId i -> i
    let getGuildId = function
        | UniqueUser (_, guildId) -> match guildId with GuildId i -> i

    let userExists user =
        let sql = """
            SELECT CASE WHEN EXISTS (
                SELECT *
                FROM DIJON_MEMBER_RECORDS
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
        dapperHelpers.QuerySingle<bool>(sql, data)

    let createUser (user: MemberUpdate) =
        let sql = """
            INSERT INTO DIJON_MEMBER_RECORDS (
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
        dapperHelpers.Execute(sql, data)

    let updateUser (updatedMember: MemberUpdate) =
        let sql = """
            UPDATE DIJON_MEMBER_RECORDS SET
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

        dapperHelpers.ExecuteReader(sql, data)

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

    interface IGuildMembersDatabase with
        member x.ListAsync guildId =
            let sql = """
                SELECT * FROM DIJON_MEMBER_RECORDS WHERE GuildId = @id
            """
            let data = dict ["id" => match guildId with GuildId i -> i]

            dapperHelpers.ExecuteReader(sql, data)
            |> Async.AwaitTask
            |> Async.Map mapReaderToUsers

        member x.BatchSetAsync members =
            // Cheating for now until I can get a batch update in
            members
            |> Seq.map (fun m -> task {
                let! userExists = userExists (UniqueUser.FromMemberUpdate m)

                if not userExists then
                    do! dapperHelpers.IgnoreResult(createUser m)
                else
                    do! dapperHelpers.IgnoreResult(updateUser m)
            })
            |> Seq.map Async.AwaitTask
            |> Async.Parallel
            // TODO: Async.Ignore will drop exceptions if any are thrown
            |> Async.Ignore

        member x.DeleteAsync user =
            let sql = """
                DELETE FROM DIJON_MEMBER_RECORDS WHERE GuildId = @guildId AND DiscordId = @discordId
            """
            let data = dict [
                "guildId" => getGuildId user
                "discordId" => getDiscordId user
            ]

            dapperHelpers.Execute(sql, data)
            |> dapperHelpers.IgnoreResult
            |> Async.AwaitTask
    end
