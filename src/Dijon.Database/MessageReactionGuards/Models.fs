namespace Dijon.Database.MessageReactionGuards

type ReferencedMessage =
    {
        GuildId: int64
        MessageId: int64
        ChannelId: int64
    }

type ReactionGuardedMessage =
    {
        Id: int
        GuildId: int64
        MessageId: int64
        ChannelId: int64
    }
