namespace Dijon.Migrations

open SimpleMigrations

[<Migration(2L, "Add DIJON_AFFIXES_CHANNELS.LastAffixesPosted column")>]
type Migration_02() =
    inherit Migration() with
        override x.Down() =
            x.Execute "ALTER TABLE DIJON_AFFIXES_CHANNELS DROP COLUMN [LastAffixesPosted]"
            
        override x.Up() =
            x.Execute "ALTER TABLE DIJON_AFFIXES_CHANNELS ADD [LastAffixesPosted] nvarchar(1000)"