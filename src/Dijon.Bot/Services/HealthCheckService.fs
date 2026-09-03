namespace Dijon.Bot.Services

open Dijon.Bot

open System
open System.IO
open System.Text
open System.IO.Pipes
open System.Threading
open System.Threading.Tasks
open Discord
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

/// Background service that listens on a named pipe for health check requests.
/// Responds with "healthy" or "unhealthy" based on the bot's Discord connection state.
type PipeHealthWorker(
    bot: IBotClient,
    logger: ILogger<PipeHealthWorker>
) =
    inherit BackgroundService()

    static member PipeName = "dijon-bot-health"

    override _.ExecuteAsync(stoppingToken: CancellationToken) =
        task {
            logger.LogInformation("Health check pipe server starting on pipe '{PipeName}'", PipeHealthWorker.PipeName)
            while not stoppingToken.IsCancellationRequested do
                try
                    use server = new NamedPipeServerStream(
                        PipeHealthWorker.PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None
                    )

                    try
                        do! server.WaitForConnectionAsync(stoppingToken)
                    with :? OperationCanceledException ->
                        return ()

                    if not stoppingToken.IsCancellationRequested then
                        try
                            use reader = new StreamReader(server, Encoding.UTF8, true, -1, true)
                            use writer = new StreamWriter(server, Encoding.UTF8, -1, true)

                            let! _ = reader.ReadLineAsync(stoppingToken)
                            let state = bot.GetConnectionState()
                            let response =
                                if state = ConnectionState.Connected then "healthy"
                                else "unhealthy"

                            writer.WriteLine(response)
                            writer.Flush()
                        with ex ->
                            logger.LogWarning(ex, "Error handling health check connection")
                with ex ->
                    if not stoppingToken.IsCancellationRequested then
                        logger.LogError(ex, "Error in health check pipe server")
                        do! Task.Delay(1000, stoppingToken)
        }
