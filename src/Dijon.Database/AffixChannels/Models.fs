namespace Dijon.Database.AffixChannels

type AffixChannel =
    {
        GuildId: int64
        ChannelId: int64
        /// The title of the last affix message posted to this channel.
        LastAffixesPosted: string option
    }

