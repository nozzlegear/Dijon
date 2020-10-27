namespace Dijon.Services

open System
open System.Threading.Tasks
open Dijon
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System.Threading

type AffixCheckService(logger : ILogger<AffixCheckService>, bot : Dijon.BotClient, database : IDijonDatabase) =
    let mutable timer : Timer option = None
    
    let checkAffixes _ : unit =
        async {
            let! channels = database.ListAllAffixChannels()
            
            if List.isEmpty channels then
                logger.LogInformation(sprintf "No guilds have enabled the affixes channel, no reason to check affixes.")
            else
                let! newAffixes = Affixes.list()
                
                match newAffixes with
                | Error err ->
                    logger.LogError(sprintf "Failed to get new affixes due to reason: %s" err)
                | Ok affixes ->
                    logger.LogInformation(sprintf "Got affixes: %s" affixes.title)
                    
                    for (guildId, channelId) in channels do
                        do! bot.PostAffixesMessageAsync guildId channelId affixes
                
        } |> Async.RunSynchronously
    
    interface IDisposable with
        member x.Dispose() =
            timer
            |> Option.iter (fun timer -> timer.Dispose())

    interface IHostedService with
        member x.StartAsync cancellationToken =
            let baseTimer = new Timer(checkAffixes, None, TimeSpan.Zero, TimeSpan.FromMinutes 1.)
            timer <- Some baseTimer
            
            Task.CompletedTask
            
        member x.StopAsync cancellationToken =
            timer
            |> Option.iter (fun timer -> timer.Change(Timeout.Infinite, 0) |> ignore)
            
            Task.CompletedTask
