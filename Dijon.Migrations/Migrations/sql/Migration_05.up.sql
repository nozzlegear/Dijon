CREATE PROC [sp_ListStreamAnnouncementMessagesForStreamer]
    (@streamerId bigint)
AS
BEGIN
    if (@streamerId is null)
        begin
            RAISERROR('@streamerId cannot be null', 18, 0)
            return
        end

    SELECT * FROM [DIJON_STREAM_ANNOUNCEMENT_MESSAGES] WHERE [StreamerId] = @streamerId;
END;

GO;

CREATE PROC [sp_ListStreamAnnouncementMessagesForGuild]
    (@guildId bigint)
AS
BEGIN
    if (@guildId is null)
        begin
            RAISERROR('@guildId cannot be null', 18, 0)
            return
        end

    SELECT * FROM [DIJON_STREAM_ANNOUNCEMENT_MESSAGES] WHERE [GuildId] = @guildId;
END

GO;

CREATE PROC [sp_ListStreamAnnouncementChannels]
AS
BEGIN
    SELECT * FROM [DIJON_STREAM_ANNOUNCEMENT_CHANNELS]
END

GO;

CREATE PROC [sp_GetStreamAnnouncementChannelForGuild]
    (@guildId bigint)
AS
BEGIN
    if (@guildId is null)
        begin
            RAISERROR('@guildId cannot be null', 18, 0)
            return
        end

    SELECT * FROM [DIJON_STREAM_ANNOUNCEMENT_CHANNELS] WHERE [GuildId] = @guildId
END

GO;
