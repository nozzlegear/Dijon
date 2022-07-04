namespace Dijon.Migrations

open SimpleMigrations

[<Migration(06L, "Update sp_SetStreamChannelForGuild to update row if it already exists")>]
type Migration_06() =
    inherit Migration() with
        member private x.Run sql =
            x.Execute sql

        override x.Up() = 
            Utils.readSqlFileBatches "Migration_06.up.sql"
            |> Seq.iter x.Run

        override x.Down() = 
            Utils.readSqlFileBatches "Migration_06.down.sql"
            |> Seq.iter x.Run
