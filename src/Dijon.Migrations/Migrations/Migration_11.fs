namespace Dijon.Migrations

open SimpleMigrations

[<Migration(11L, "Fix return type of sp_MessageIsReactionGuarded")>]
type Migration_11() =
    inherit Migration() with
        member private x.Run sql =
            x.Execute sql

        override x.Up() = 
            Utils.readSqlFileBatches "Migration_11.up.sql"
            |> Seq.iter x.Run

        override x.Down() = 
            Utils.readSqlFileBatches "Migration_11.down.sql"
            |> Seq.iter x.Run
