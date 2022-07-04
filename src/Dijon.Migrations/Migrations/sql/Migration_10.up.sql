CREATE TABLE [dbo].[DIJON_REACTION_GUARDED_MESSAGES] (
    Id int identity(1,1) primary key,
    GuildId bigint not null,
    ChannelId bigint not null,
    MessageId bigint not null index idx_messageid
)

GO;

CREATE PROC [sp_AddReactionGuardedMessage]
    (@guildId bigint, @messageId bigint, @channelId bigint)
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

    INSERT INTO [DIJON_REACTION_GUARDED_MESSAGES] (
        GuildId, 
        ChannelId,
        MessageId
    ) VALUES (
        @guildId, 
        @channelId,
        @messageId
    );
END;

GO;

CREATE PROC [sp_RemoveReactionGuardedMessage]
    (@messageId bigint)
AS
BEGIN
    if (@messageId is null)
        begin
            RAISERROR('@messageId cannot be null', 18, 0)
            return
        end

    DELETE FROM [DIJON_REACTION_GUARDED_MESSAGES] WHERE [MessageId] = @messageId;
END

GO;

CREATE PROC [sp_MessageIsReactionGuarded]
    (@messageId bigint)
AS
BEGIN
    if (@messageId is null)
        begin
            RAISERROR('@messageId cannot be null', 18, 0)
            return
        end

    select 1 as IsReactionGuarded from [DIJON_REACTION_GUARDED_MESSAGES] where [MessageId] = @messageId
END

GO;
