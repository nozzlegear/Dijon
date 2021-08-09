ALTER PROC [sp_SetStreamChannelForGuild]
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

    INSERT INTO [DIJON_STREAM_ANNOUNCEMENTS_CHANNELS] (
        GuildId, 
        ChannelId
    ) VALUES (
        @guildId, 
        @channelId
    );
END;

GO;
