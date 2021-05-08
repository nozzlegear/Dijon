namespace Dijon.Services

open System
open System.Threading.Tasks
open Dijon
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System.Threading

type AffixCheckService(logger : ILogger<AffixCheckService>, bot : Dijon.BotClient, database : IDijonDatabase) =
    let mutable timer : Timer option = None
   
    // The timer should run every 25 minutes on Tuesdays
    let timerRunPeriod = TimeSpan.FromMinutes 25.
     
    let adjustTimer () =
        let now =
            DateTime.UtcNow
        
        match now.DayOfWeek with
        | DayOfWeek.Tuesday ->
            // It's currently Tuesday, so we don't need to adjust the timer. It will continue running
            // every 20 minutes (based on the `timerRunPeriod` variable)
            ()
        | _ ->
            // Set the timer to pause until next Tuesday 
            // NOTE: it's important to use `now.Date.AddDays` instead of `now.AddDays` --
            // this will make sure the clock is set to midnight, and adding hours later lets us
            // pick a specific time on the day
            let mutable nextDay = now.Date.AddDays 1.
            
            while nextDay.DayOfWeek <> DayOfWeek.Tuesday do
                nextDay <- nextDay.AddDays 1.
                
            // Don't start the 20 minute checks until 15:00 UTC (9 or 10am central)
            let nextRun = (nextDay.AddHours 15.) - now
            
            logger.LogInformation(sprintf "Next affix check will occur in %.1f hours." nextRun.TotalHours)
            
            timer
            |> Option.iter (fun timer -> timer.Change(nextRun, timerRunPeriod) |> ignore)
    
    let rec postAffixes (affixes : RaiderIo.ListAffixesResponse) channels =
        match channels with
        | [] ->
            Async.Empty
        | channel :: remaining ->
            // Only post this message if it has not been posted to the guild/channel
            match channel.LastAffixesPosted with
            | Some title when title = affixes.title ->
                postAffixes affixes remaining 
            | _ ->
                let guildId = GuildId channel.GuildId
                
                async {
                    do! bot.PostAffixesMessageAsync guildId channel.ChannelId affixes
                    do! database.SetLastAffixesPostedForGuild guildId affixes.title
                    // Post to the remaining channels
                    do! postAffixes affixes remaining
                }
    
    let checkAffixes _ : unit =
        async {
            let! channels = database.ListAllAffixChannels()
            
            if List.isEmpty channels then
                logger.LogInformation(sprintf "No guilds have enabled the affixes channel, no reason to check affixes.")
            else
                match! Affixes.list() with
                | Error err ->
                    logger.LogError(sprintf "Failed to get new affixes due to reason: %s" err)
                | Ok affixes ->
                    logger.LogInformation(sprintf "Got affixes: %s" affixes.title)
                    do! postAffixes affixes channels
                    
            adjustTimer ()
        } |> Async.RunSynchronously
    
    interface IDisposable with
        member x.Dispose() =
            timer
            |> Option.iter (fun timer -> timer.Dispose())

    interface IHostedService with
        member x.StartAsync cancellationToken =
            let baseTimer = new Timer(checkAffixes, None, TimeSpan.Zero, timerRunPeriod)
            timer <- Some baseTimer
            
            Task.CompletedTask
            
        member x.StopAsync cancellationToken =
            timer
            |> Option.iter (fun timer -> timer.Change(Timeout.Infinite, 0) |> ignore)
            
            Task.CompletedTask
