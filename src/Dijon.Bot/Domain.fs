namespace Dijon.Bot

open System.ComponentModel.DataAnnotations
open Discord

type Command = 
    | Ignore 
    | TestStreamStarted
    | TestStreamEnded
    | TestUserLeft
    | Goulash 
    | Status 
    | SetLogChannel 
    | Slander 
    | Hype
    | Help
    | Unknown
    | AidAgainstSlander
    | FoxyLocation
    | GetAffix
    | SetAffixesChannel
    | SetStreamsChannel
    | UnsetStreamsChannel
    | AddMessageReactionGuard
    | RemoveMessageReactionGuard

module RaiderIo =
    open Thoth.Json.Net
    type Affix =
        {
            id: int
            name: string
            description: string
            wowhead_url: string
        }
        with
        static member Decoder : Decoder<Affix> =
            Decode.object (fun get ->
                { id = get.Required.Field "id" Decode.int
                  name = get.Required.Field "name" Decode.string
                  description = get.Required.Field "description" Decode.string
                  wowhead_url = get.Required.Field "wowhead_url" Decode.string } )
            
    type ListAffixesResponse =
        {
            region: string
            title: string
            leaderboard_url: string
            affix_details: Affix list 
        }
        with
        static member Decoder : Decoder<ListAffixesResponse> =
            Decode.object (fun get ->
                { region = get.Required.Field "region" Decode.string
                  title = get.Required.Field "title" Decode.string
                  leaderboard_url = get.Required.Field "leaderboard_url" Decode.string
                  affix_details = get.Required.Field "affix_details" (Decode.list Affix.Decoder)})

type BotClientOptions = {
    [<Required>]
    ApiToken: string
}

type StreamData = {
    Name: string
    Details: string
    Url: string
    User: IUser
    GuildId: int64
}

