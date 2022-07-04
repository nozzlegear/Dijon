namespace Dijon.Migrations

open SimpleMigrations

[<Migration(09L, "Change [DIJON_STREAM_ANNOUNCEMENT_MESSAGES].DateCreated to datetimeoffset")>]
type Migration_09() =
    inherit Migration() with
        member private x.Run sql =
            x.Execute sql

        override x.Up() = 
            Utils.readSqlFileBatches "Migration_09.up.sql"
            |> Seq.iter x.Run

        override x.Down() = 
            Utils.readSqlFileBatches "Migration_09.down.sql"
            |> Seq.iter x.Run
