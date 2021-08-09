namespace Dijon.Migrations

open SimpleMigrations

[<Migration(07L, "Add [Dijon_Stream_Announcements_Channels].[StreamerRoleId]")>]
type Migration_07() =
    inherit Migration() with
        member private x.Run sql =
            x.Execute sql

        override x.Up() = 
            Utils.readSqlFileBatches "Migration_07.up.sql"
            |> Seq.iter x.Run

        override x.Down() = 
            Utils.readSqlFileBatches "Migration_07.down.sql"
            |> Seq.iter x.Run
