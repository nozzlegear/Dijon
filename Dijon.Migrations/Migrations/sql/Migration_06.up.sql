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

    -- If the channel already exists, just update it
    if exists (select 1 from [DIJON_STREAM_ANNOUNCEMENT_CHANNELS] where [GuildId] = @guildId)
        begin
            UPDATE [DIJON_STREAM_ANNOUNCEMENT_CHANNELS]
            SET [ChannelId] = @channelId
            WHERE [GuildId] = @guildId;
        end
    else
        begin
            INSERT INTO [DIJON_STREAM_ANNOUNCEMENT_CHANNELS] (
                GuildId, 
                ChannelId
            ) VALUES (
                @guildId, 
                @channelId
            );
        end
END;

GO;
