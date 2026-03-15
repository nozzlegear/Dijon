namespace Dijon.Migrations

open SimpleMigrations

[<Migration(1L, "Initial database schema")>]
type Migration_01() =
    inherit Migration() with
        override x.Down() =
            x.Execute "DROP TABLE IF EXISTS dijon_reaction_guarded_messages"
            x.Execute "DROP TABLE IF EXISTS dijon_stream_announcement_messages"
            x.Execute "DROP TABLE IF EXISTS dijon_stream_announcement_channels"
            x.Execute "DROP TABLE IF EXISTS dijon_affix_channels"
            x.Execute "DROP TABLE IF EXISTS dijon_log_channels"

        override x.Up() =
            x.Execute """
                CREATE TABLE dijon_log_channels (
                    id         SERIAL PRIMARY KEY,
                    guild_id   BIGINT NOT NULL,
                    channel_id BIGINT NOT NULL,
                    CONSTRAINT uq_log_channels_guild_id UNIQUE (guild_id)
                )
            """
            x.Execute """
                CREATE TABLE dijon_affix_channels (
                    id                  SERIAL PRIMARY KEY,
                    guild_id            BIGINT NOT NULL,
                    channel_id          BIGINT NOT NULL,
                    last_affixes_posted VARCHAR(1000) NULL,
                    CONSTRAINT uq_affix_channels_guild_id UNIQUE (guild_id)
                )
            """
            x.Execute """
                CREATE TABLE dijon_stream_announcement_channels (
                    id               SERIAL PRIMARY KEY,
                    guild_id         BIGINT NOT NULL,
                    channel_id       BIGINT NOT NULL,
                    streamer_role_id BIGINT NOT NULL,
                    CONSTRAINT uq_stream_channels_guild_id UNIQUE (guild_id)
                )
            """
            x.Execute """
                CREATE TABLE dijon_stream_announcement_messages (
                    id           SERIAL PRIMARY KEY,
                    date_created TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    guild_id     BIGINT NOT NULL,
                    channel_id   BIGINT NOT NULL,
                    message_id   BIGINT NOT NULL,
                    streamer_id  BIGINT NOT NULL
                )
            """
            x.Execute "CREATE INDEX idx_stream_messages_streamer_id ON dijon_stream_announcement_messages (streamer_id)"
            x.Execute """
                CREATE TABLE dijon_reaction_guarded_messages (
                    id         SERIAL PRIMARY KEY,
                    guild_id   BIGINT NOT NULL,
                    channel_id BIGINT NOT NULL,
                    message_id BIGINT NOT NULL
                )
            """
            x.Execute "CREATE INDEX idx_reaction_guards_message_id ON dijon_reaction_guarded_messages (message_id)"
