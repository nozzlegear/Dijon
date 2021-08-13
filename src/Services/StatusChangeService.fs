namespace Dijon.Services

open Dijon
open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type StatusChangeService(logger : ILogger<StatusChangeService>, bot : Dijon.BotClient) =
    let mutable timer : System.Timers.Timer option = None
    
    let changeStatus () : unit =
        async {
            let status =
                [ "Incentives Available Subject to Covenant"
                  "Exceedingly Tedious and Slightly Unhinged"
                  "Sweet and Full of Grace"
                  "Now We Try It My Way"
                  "This Is Legal But We Question The Ethics"
                  "Conditions to Deteriorate Soon"
                  "Moderate Traffic Density"
                  "I Don't Need A Tie For Gravitas"
                  "Flatten The Curve"
                  "A Virulent Surplus of Hubris"
                  "No Scrap Value"
                  "Failure of Statecraft"
                  "Down With Biggelsworth!"
                  "Decentralize me, daddy" ]
                |> Seq.sortBy (fun _ -> Guid.NewGuid())
                |> Seq.head
            do! bot.UpdateGameAsync status |> Async.AwaitTask
        } |> Async.Start
        
    let rec scheduleJob (cancellation : CancellationToken) startNow =
        let baseTimer = new System.Timers.Timer(TimeSpan.FromHours(1.).TotalMilliseconds)
        // Set AutoReset to false so the event is only raised once per timer
        baseTimer.AutoReset <- false
        timer <- Some baseTimer
        
        baseTimer.Elapsed
        |> Event.add (fun _ ->
            baseTimer.Dispose()
            timer <- None
            
            if not cancellation.IsCancellationRequested then
                changeStatus()
            
            // Schedule the next job as soon as this one fires
            if not cancellation.IsCancellationRequested then
                scheduleJob cancellation false
        )
        
        baseTimer.Start()
        
        if startNow then
            changeStatus()

    interface IDisposable with
        member _.Dispose() = 
            timer
            |> Option.iter (fun timer -> timer.Dispose())

    interface IHostedService with
        member _.StartAsync cancellation =
            scheduleJob cancellation true
            Task.CompletedTask

        member _.StopAsync _ =
            timer
            |> Option.iter (fun timer -> timer.Stop())
            Task.CompletedTask
