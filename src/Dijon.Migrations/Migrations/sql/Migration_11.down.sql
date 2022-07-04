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
