namespace Dijon.Services

open System.Threading.Tasks
open Dijon
open Dijon.Migrations
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type DatabaseMigratorService (logger: ILogger<DatabaseMigratorService>, dbOptions: DatabaseOptions) =
    
    interface IHostedService with
        member x.StartAsync cancellation =
            logger.LogInformation "Migrating database to latest version."
            Migrator.migrate Migrator.Latest dbOptions.ConnectionString
            Task.CompletedTask

        member x.StopAsync cancellation =
            Task.CompletedTask
