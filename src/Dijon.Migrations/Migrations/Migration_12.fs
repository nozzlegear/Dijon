namespace Dijon.Migrations

open SimpleMigrations

[<Migration(12L, "Drop DIJON_MEMBER_RECORDS table")>]
type Migration_12() =
    inherit Migration() with
        member private x.Run sql =
            x.Execute sql

        override x.Up() =
            Utils.readSqlFileBatches "Migration_12.up.sql"
            |> Seq.iter x.Run

        override x.Down() =
            Utils.readSqlFileBatches "Migration_12.down.sql"
            |> Seq.iter x.Run
