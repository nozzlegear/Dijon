namespace Dijon.Migrations

open SimpleMigrations
open SimpleMigrations.DatabaseProvider
open Npgsql

module Migrator =
    type MigrationTarget =
        | Latest
        | Baseline of int64
        | Target of int64

    let migrate direction (connStr : string) =
        let assembly = typeof<Migration_01>.Assembly

        use connection = new NpgsqlConnection(connStr)
        connection.Open()
        let provider = PostgresqlDatabaseProvider connection
        // Customize the name of the migration history table
        provider.TableName <- "dijon_migrations"
        let migrator = SimpleMigrator(assembly, provider)

        migrator.Load()

        match direction with
        | Latest ->
            migrator.MigrateToLatest()
        | Baseline target ->
            migrator.Baseline target
        | Target target ->
            migrator.MigrateTo target
