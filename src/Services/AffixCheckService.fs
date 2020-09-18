namespace Dijon.Services

open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System.Threading

type AffixCheckService(logger : ILogger<AffixCheckService>, bot : Dijon.BotClient) =
    let mutable timer : Timer option = None
    
    let checkAffixes _ : unit =
        let newAffixes =
            Affixes.list()
            |> Async.RunSynchronously
            
        let someName = Some "Joshua"
        
        match someName with
        | Some x ->
            // do something with x
            ()
        | None ->
            // name has no value set
            ()
            
        match newAffixes with
        | Error err ->
            logger.LogError(sprintf "Failed to get new affixes due to reason: %s" err)
        | Ok affixes ->
            logger.LogInformation(sprintf "Got affixes: %s" affixes.title)
    
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
