ALTER PROC [sp_MessageIsReactionGuarded]
    (@messageId bigint)
AS
BEGIN
    if (@messageId is null)
        begin
            RAISERROR('@messageId cannot be null', 18, 0)
            return
        end

    IF EXISTS (select 1 from [DIJON_REACTION_GUARDED_MESSAGES] where [MessageId] = @messageId)
        select (cast (1 as bit)) as IsReactionGuarded
    ELSE
        select (cast (0 as bit)) as IsReactionGuarded
END;

GO;
