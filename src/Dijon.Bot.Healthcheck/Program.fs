module Dijon.Bot.Healthcheck.Program

open System
open System.IO
open System.IO.Pipes
open System.Threading
open System.Threading.Tasks

/// Connects to the Dijon.Bot named pipe and checks health status.
/// Exit 0 = healthy, 1 = unhealthy/timeout/connection failure.
[<EntryPoint>]
let main _ =
    let pipeName = "dijon-bot-health"

    use cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.0))

    let result =
        task {
            try
                use client = new NamedPipeClientStream(
                    ".",
                    pipeName,
                    PipeDirection.InOut,
                    PipeOptions.None
                )

                do! client.ConnectAsync(cts.Token)

                use writer = new StreamWriter(client, leaveOpen = true)
                use reader = new StreamReader(client, leaveOpen = true)

                writer.WriteLine("ping")
                writer.Flush()

                let! response = reader.ReadLineAsync(cts.Token)

                if String.Equals(response, "healthy", StringComparison.OrdinalIgnoreCase) then
                    return 0
                else
                    return 1
            with
            | :? OperationCanceledException ->
                return 1
            | _ ->
                return 1
        }

    result.GetAwaiter().GetResult()
