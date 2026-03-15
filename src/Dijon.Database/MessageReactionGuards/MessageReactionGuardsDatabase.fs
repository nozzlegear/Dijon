namespace Dijon.Database.MessageReactionGuards

open Dijon.Database
open Dijon.Shared

open Npgsql.FSharp
open Microsoft.Extensions.Options
open System.Threading.Tasks

type IMessageReactionGuardDatabase =
    abstract member MessageIsReactionGuarded: messageId: int64 -> Task<bool>
    abstract member AddReactionGuardedMessage: referenceMessage: ReferencedMessage -> Task<unit>
    abstract member RemoveReactionGuardedMessage: messageId: int64 -> Task<unit>

type MessageReactionGuardDatabase(options: IOptions<ConnectionStrings>) =
    let connectionString = options.Value.DefaultConnection

    interface IMessageReactionGuardDatabase with
        member _.MessageIsReactionGuarded messageId =
            connectionString
            |> Sql.connect
            |> Sql.query """
                SELECT EXISTS(
                    SELECT 1 FROM dijon_reaction_guarded_messages WHERE message_id = @messageId
                ) AS is_guarded
            """
            |> Sql.parameters [ "messageId", Sql.int64 messageId ]
            |> Sql.executeAsync (fun r -> r.bool "is_guarded")
            |> Task.map List.head

        member _.AddReactionGuardedMessage message =
            connectionString
            |> Sql.connect
            |> Sql.query """
                INSERT INTO dijon_reaction_guarded_messages (guild_id, channel_id, message_id)
                VALUES (@guildId, @channelId, @messageId)
            """
            |> Sql.parameters
                [ "guildId", Sql.int64 message.GuildId
                  "channelId", Sql.int64 message.ChannelId
                  "messageId", Sql.int64 message.MessageId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.RemoveReactionGuardedMessage messageId =
            connectionString
            |> Sql.connect
            |> Sql.query "DELETE FROM dijon_reaction_guarded_messages WHERE message_id = @messageId"
            |> Sql.parameters [ "messageId", Sql.int64 messageId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore
    end
