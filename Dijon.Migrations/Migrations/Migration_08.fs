namespace Dijon.Migrations

open SimpleMigrations

[<Migration(08L, "Rename procs for setting/unsetting stream announcement channels")>]
type Migration_08() =
    inherit Migration() with
        member private x.Run sql =
            x.Execute sql

        override x.Up() = 
            Utils.readSqlFileBatches "Migration_08.up.sql"
            |> Seq.iter x.Run

        override x.Down() = 
            Utils.readSqlFileBatches "Migration_08.down.sql"
            |> Seq.iter x.Run
