IF NOT EXISTS (SELECT * FROM sys.tables
WHERE name = N'DIJON_STREAM_ANNOUNCEMENT_CHANNELS' AND type = 'U')

BEGIN
CREATE TABLE [dbo].[DIJON_STREAM_ANNOUNCEMENT_CHANNELS] (
    Id int identity(1,1) primary key,
    GuildId bigint not null index idx_guildid,
    ChannelId bigint not null
)
END;

IF NOT EXISTS (SELECT * FROM sys.tables
WHERE name = N'DIJON_STREAM_ANNOUNCEMENT_MESSAGES' AND type = 'U')

BEGIN
CREATE TABLE [dbo].[DIJON_STREAM_ANNOUNCEMENT_MESSAGES] (
    Id int identity(1,1) primary key,
    DateCreated datetime2 not null,
    GuildId bigint not null,
    ChannelId bigint not null,
    MessageId bigint not null,
    StreamerId bigint not null index idx_streamerid
)
END;
