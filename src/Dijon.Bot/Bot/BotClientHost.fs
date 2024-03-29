namespace Dijon.Bot

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System
open System.Threading.Tasks

type BotClientHost(
    bot: IBotClient,
    logger: ILogger<BotClientHost>
) =
    interface IHostedService with
        member _.StartAsync cancellationToken =
            logger.LogTrace("Initializing Bot")
            bot.InitAsync (cancellationToken)

        member _.StopAsync _ =
            Task.CompletedTask
    end
