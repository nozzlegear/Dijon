namespace Dijon.Bot

open System.Threading.Tasks

module Affixes =
    open Dijon.Bot
    open System
    open System.Net.Http
    
    let list () : Task<Result<RaiderIo.ListAffixesResponse, string>> =
        task {
            use client = new HttpClient()
            use req = new HttpRequestMessage()
            req.Method <- HttpMethod.Get
            req.RequestUri <- Uri "https://raider.io/api/v1/mythic-plus/affixes?region=us"
            
            let! result = client.SendAsync req

            if not result.IsSuccessStatusCode then
                let msg = sprintf "Failed to list affixes. Raider.io returned %i %s." (int result.StatusCode) result.ReasonPhrase
                return Error msg
            else
                let! content = result.Content.ReadAsStringAsync()
                return Thoth.Json.Net.Decode.fromString RaiderIo.ListAffixesResponse.Decoder content
        }
