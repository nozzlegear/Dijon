namespace Dijon.Migrations

open SimpleMigrations

[<Migration(03L, "Add stream announcements config/messages tables")>]
type Migration_03() =
    inherit Migration() with
        member private x.Run (sql : string) =
            x.Execute sql

        override x.Up() = 
            Utils.readSqlFileBatches "Migration_03.up.sql"
            |> Seq.iter x.Run

        override x.Down() = 
            x.Execute "test"
            Utils.readSqlFileBatches "Migration_03.down.sql"
            |> Seq.iter x.Run
