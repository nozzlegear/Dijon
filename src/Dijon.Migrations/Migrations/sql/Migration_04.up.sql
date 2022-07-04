CREATE PROC [sp_SetStreamChannelForGuild]
    (@guildId bigint, @channelId bigint)
AS
BEGIN
    if (@guildId is null)
        begin
            RAISERROR('@guildId cannot be null', 18, 0)
            return
        end

    if (@channelId is null)
        begin
            RAISERROR('@channelId cannot be null', 18, 0)
            return
        end

    INSERT INTO [DIJON_STREAM_ANNOUNCEMENT_CHANNELS] (
        GuildId, 
        ChannelId
    ) VALUES (
        @guildId, 
        @channelId
    );
END;

GO

CREATE PROC [sp_UnsetStreamChannelForGuild]
    (@guildId bigint)
AS
BEGIN
    if (@guildId is null)
        begin
            RAISERROR('@guildId cannot be null', 18, 0)
            return
        end

    DELETE FROM [DIJON_STREAM_ANNOUNCEMENT_CHANNELS] WHERE [GuildId] = @guildId;
END;

GO

CREATE PROC [sp_AddStreamAnnouncementMessage]
    (@guildId bigint, @channelId bigint, @messageId bigint, @streamerId bigint)
AS
BEGIN
    if (@guildId is null)
        begin
            RAISERROR('@guildId cannot be null', 18, 0)
            return
        end

    if (@channelId is null)
        begin
            RAISERROR('@channelId cannot be null', 18, 0)
            return
        end

    if (@messageId is null)
        begin
            RAISERROR('@messageId cannot be null', 18, 0)
            return
        end

    if (@streamerId is null)
        begin
            RAISERROR('@streamerId cannot be null', 18, 0)
            return
        end

    INSERT INTO [DIJON_STREAM_ANNOUNCEMENT_MESSAGES] (
        DateCreated,
        GuildId,
        ChannelId,
        MessageId,
        StreamerId
    ) VALUES (
        sysdatetimeoffset(),
        @guildId,
        @channelId,
        @messageId,
        @streamerId
    );
END;

GO

CREATE PROC [sp_DeleteStreamAnnouncementMessageForStreamer]
    (@streamerId bigint)
AS
BEGIN
    if (@streamerId is null)
        begin
            RAISERROR('@streamerId cannot be null', 18, 0)
            return
        end

    DELETE FROM [DIJON_STREAM_ANNOUNCEMENT_MESSAGES] WHERE [StreamerId] = @streamerId;
END;

GO
