namespace Dijon.Bot.Services

open Dijon.Database
open Dijon.Migrations

open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options

type DatabaseMigratorService (
    logger: ILogger<DatabaseMigratorService>,
    dbOptions: IOptions<ConnectionStrings>
) =
    
    interface IHostedService with
        member x.StartAsync _ =
            logger.LogInformation "Migrating database to latest version."
            Migrator.migrate Migrator.Latest dbOptions.Value.DefaultConnection
            Task.CompletedTask

        member x.StopAsync _ =
            Task.CompletedTask
