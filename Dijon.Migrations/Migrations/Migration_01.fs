namespace Dijon.Migrations

open SimpleMigrations

[<Migration(1L, "Initial database migration")>]
type Migration_01() =
    inherit Migration() with
        override x.Down() =
            x.Execute "DROP TABLE DIJON_MEMBER_RECORDS"
            x.Execute "DROP TABLE DIJON_LOG_CHANNELS"
            x.Execute "DROP TABLE DIJON_AFFIXES_CHANNELS"
        
        override x.Up() =
            x.Execute """
                IF NOT EXISTS (SELECT * FROM sys.tables
                WHERE name = N'DIJON_MEMBER_RECORDS' AND type = 'U')

                BEGIN
                CREATE TABLE [dbo].[DIJON_MEMBER_RECORDS] (
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
            x.Execute """
                IF NOT EXISTS (SELECT * FROM sys.tables
                WHERE name = N'DIJON_LOG_CHANNELS' AND type = 'U')

                BEGIN
                CREATE TABLE [dbo].[DIJON_LOG_CHANNELS] (
                    Id int identity(1,1) primary key,
                    GuildId bigint not null index idx_guildid,
                    ChannelId bigint not null
                )
                END
            """
            x.Execute """
                IF NOT EXISTS (SELECT * FROM sys.tables
                WHERE name = N'DIJON_AFFIXES_CHANNELS' AND type = 'U')
                
                BEGIN
                CREATE TABLE [dbo].[DIJON_AFFIXES_CHANNELS] (
                    Id int identity(1,1) primary key,
                    GuildId bigint not null index idx_guildid,
                    ChannelId bigint not null
                )
                END
            """