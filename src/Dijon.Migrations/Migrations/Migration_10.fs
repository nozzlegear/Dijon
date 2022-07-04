namespace Dijon.Migrations

open SimpleMigrations

[<Migration(10L, "Change [DIJON_STREAM_ANNOUNCEMENT_MESSAGES].DateCreated to datetimeoffset")>]
type Migration_10() =
    inherit Migration() with
        member private x.Run sql =
            x.Execute sql

        override x.Up() = 
            Utils.readSqlFileBatches "Migration_10.up.sql"
            |> Seq.iter x.Run

        override x.Down() = 
            Utils.readSqlFileBatches "Migration_10.down.sql"
            |> Seq.iter x.Run
