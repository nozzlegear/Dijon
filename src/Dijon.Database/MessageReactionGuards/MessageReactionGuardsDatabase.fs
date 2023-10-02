namespace Dijon.Database.MessageReactionGuards

open Dijon.Database
open Dijon.Shared

open DustyTables
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
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_MessageIsReactionGuarded"
            |> Sql.parameters
                [ "@messageId", Sql.int64 messageId ]
            |> Sql.executeRowAsync (fun r -> r.bool "IsReactionGuarded")

        member _.AddReactionGuardedMessage message =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_AddReactionGuardedMessage"
            |> Sql.parameters
                [ "@guildId", Sql.int64 message.GuildId
                  "@channelId", Sql.int64 message.ChannelId
                  "@messageId", Sql.int64 message.MessageId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore

        member _.RemoveReactionGuardedMessage messageId =
            Sql.connect connectionString
            |> Sql.storedProcedure "sp_RemoveReactionGuardedMessage"
            |> Sql.parameters
                [ "@messageId", Sql.int64 messageId ]
            |> Sql.executeNonQueryAsync
            |> Task.ignore
    end
