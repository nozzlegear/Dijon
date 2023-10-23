namespace Dijon.Bot

open System
open System.Threading.Tasks
open Microsoft.Extensions.Hosting

type BotClientHost(
    bot: IBotClient
) =
    interface IHostedService with
        member _.StartAsync cancellationToken =
            bot.InitAsync (cancellationToken)

        member _.StopAsync _ =
            match bot with
            | :? IAsyncDisposable as x ->
                x.DisposeAsync().AsTask()
            | _ ->
                Task.CompletedTask
    end
