ALTER TABLE [DIJON_STREAM_ANNOUNCEMENT_CHANNELS] 
    ADD [StreamerRoleId] bigint not null;

GO;

ALTER PROC [sp_SetStreamChannelForGuild]
    (@guildId bigint, @channelId bigint, @streamerRoleId bigint)
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

    if (@streamerRoleId is null)
        begin
            RAISERROR('@streamerRoleId cannot be null', 18, 0)
            return
        end

    -- If the channel already exists, just update it
    if exists (select 1 from [DIJON_STREAM_ANNOUNCEMENT_CHANNELS] where [GuildId] = @guildId)
        begin
            UPDATE [DIJON_STREAM_ANNOUNCEMENT_CHANNELS]
            SET [ChannelId] = @channelId,
                [StreamerRoleId] = @streamerRoleId
            WHERE [GuildId] = @guildId;
        end
    else
        begin
            INSERT INTO [DIJON_STREAM_ANNOUNCEMENT_CHANNELS] (
                GuildId, 
                ChannelId,
                StreamerRoleId
            ) VALUES (
                @guildId, 
                @channelId,
                @streamerRoleId
            );
        end
END;

GO;
