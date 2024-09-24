CREATE TABLE [dbo].[DIJON_MEMBER_RECORDS] (
    Id int identity(1,1) primary key,
    DiscordId bigint not null index idx_discordid,
    GuildId bigint not null index idx_guildid,
    FirstSeenAt datetime2 not null,
    Username nvarchar(500) not null,
    Discriminator nvarchar (12) not null,
    Nickname nvarchar (1000) null
)

GO;
