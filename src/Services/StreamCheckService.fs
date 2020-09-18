namespace Dijon.Services

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type StreamCheckService(logger : ILogger<StreamCheckService>, bot : Dijon.BotClient) =
    let mutable timer : Timer option = None
    
    let checkStreams state : unit =
        logger.LogWarning "Service is checking streams."
        
        async {
            let status =
                [ "Incentives Available Subject to Covenant"
                  "Exceedingly Tedious and Slightly Unhinged"
                  "Sweet and Full of Grace"
                  "Two for Flinching"
                  "Now We Try It My Way"
                  "This Is Legal But We Question The Ethics"
                  "Conditions to Deteriorate Soon"
                  "Emergency Phone Not Installed"
                  "True Scale of Excess Mortality"
                  "Moderate Traffic Density"
                  "I Don't Need A Tie For Gravitas"
                  "Flatten The Curve"
                  "A Virulent Surplus of Hubris"
                  "No Scrap Value"
                  "Failure of Statecraft"
                  "Down With Biggelsworth!" ]
                |> Seq.sortBy (fun _ -> Guid.NewGuid())
                |> Seq.head
            do! bot.UpdateGameAsync status |> Async.AwaitTask
        } |> Async.RunSynchronously
    
    interface IDisposable with
        member x.Dispose() =
            timer
            |> Option.iter (fun timer -> timer.Dispose())
            
    interface IHostedService with
        member x.StartAsync cancellation =
            let baseTimer = new Timer(checkStreams, None, TimeSpan.Zero, TimeSpan.FromMinutes(3.))
            timer <- Some baseTimer
            
            Task.CompletedTask
            
        member x.StopAsync cancellation =
            timer
            |> Option.iter (fun timer -> timer.Change(Timeout.Infinite, 0) |> ignore)
            Task.CompletedTask