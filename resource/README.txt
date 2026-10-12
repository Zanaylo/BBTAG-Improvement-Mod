BBTAG Improvement Mod launcher
==============================

Starts BlazBlue Cross Tag Battle with mods loaded from their own folders.
Nothing in the game folder is replaced or changed, so Steam's "Verify integrity"
never undoes a mod and uninstalling is deleting this folder.

Files
-----
BBTAG_IM_Launcher.exe   Starts the game and loads the mods.
BBTAG_IM.dll            Loaded into the game by the launcher. Keep it next to the launcher.
mods\                   Put mod folders here.
logs\BBTAG_IM.log       Written on every start. Read it when something does not load.

Installing
----------
Copy BBTAG_IM_Launcher.exe, BBTAG_IM.dll and the "mods" folder into the game folder,
next to BBTAG.exe (Steam: right click the game > Manage > Browse local files):

    BBTAG\
        BBTAG.exe
        BBTAG_IM_Launcher.exe
        BBTAG_IM.dll
        mods\
            BBTAG-OverHaul-Patches\
                BBTAG.exe
                data2\

Keep the game's own BBTAG.exe and data folder unmodded; the launcher layers the mods
over them.

Using it
--------
Start Steam, then run BBTAG_IM_Launcher.exe. Starting the game from Steam or from
BBTAG.exe runs it without mods.

You can also drag one or more mod folders onto BBTAG_IM_Launcher.exe. Folders given
that way are loaded instead of the "mods" folder. The launcher can also live outside
the game folder: it then finds the game through Steam, or through --game "<folder>".
Run with --help for every option.

What a mod folder holds
-----------------------
data\ or data2\   Files laid out like the game's "data" folder. A file here is used
                  instead of the game's file with the same path, and new files are
                  added. BBTAG-OverHaul-Patches uses data2, which works as is.
BBTAG.exe         Optional. A modded copy of the game executable. Its differences from
                  your installed BBTAG.exe are applied in memory when the game starts.
                  It must be made from the same game version as your installed copy.

When two mods replace the same file, the one that comes first wins: name order inside
"mods", or the order you passed the folders in.
