namespace Dijon.Database.StreamAnnouncements

open System

type PartialStreamAnnouncementChannel =
    { GuildId: int64
      ChannelId: int64
      StreamerRoleId: int64 }

type StreamAnnouncementChannel =
    { Id: int
      GuildId: int64
      ChannelId: int64
      StreamerRoleId: int64 }

type PartialStreamAnnouncementMessage =
    {
        GuildId: int64
        ChannelId: int64
        MessageId: int64
        StreamerId: int64
    }

type StreamAnnouncementMessage =
    {
        Id: int
        DateCreated: DateTimeOffset
        GuildId: int64
        ChannelId: int64
        MessageId: int64
        StreamerId: int64
    }
