namespace Dijon.Bot.Services

open Dijon.Bot
open Dijon.Shared

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting

type StatusChangeService(
    bot: IBotClient
) =
    let mutable timer : System.Timers.Timer option = None

    let changeStatus (): unit =
        let status =
            [ "Incentives Available Subject to Covenant"
              "Exceedingly Tedious and Slightly Unhinged"
              "Sweet and Full of Grace"
              "Now We Try It My Way"
              "This Is Legal But We Question The Ethics"
              "Conditions to Deteriorate Soon"
              "Moderate Traffic Density"
              "I Don't Need A Tie For Gravitas"
              "Ascension's Red Harvest"
              "A Virulent Surplus of Hubris"
              "No Scrap Value"
              "Failure of Statecraft"
              "Down With Biggelsworth!"
              "Decentralize me, daddy"
              "They Blamed The Beasts"
              "One Must Imagine Sisyphus Happy"
              "This Wait Too Long, This Weight Too Heavy"
              "By This Blood Shall We Have Life Eternal"
              "One Batch, Two Batch"
              "Penny and Dime"
              "Crash Test Dummy"
              "Two For Flinching" ]
            |> Seq.sortBy (fun _ -> Guid.NewGuid())
            |> Seq.head
        Task.Run<unit> (fun () -> backgroundTask {
            do! bot.SetActivityStatusAsync status
        }) |> ignore

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
