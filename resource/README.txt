BBTAG Mod Loader
================

Starts BlazBlue Cross Tag Battle with mods loaded from their own folders.
The game folder is never changed: to uninstall, delete the Mod Loader files.

Installing
----------
Copy every file and the "mods" folder next to BBTAG.exe
(Steam: right click the game > Manage > Browse local files).

Using it
--------
Start Steam, run BBTAG_Mod_Loader.exe and press Play.

Mods tab: tick the mods to load and use the arrows to set the order. When two
mods replace the same file, the higher one wins. Keep BBTAG-Improvement-Mod
above BBTAG-OverHaul.

Cheat Engine tab: tick the table scripts you want. Needs Cheat Engine 7.7.
Wait for "You can start a match now" before starting a match.

In the game
-----------
    F1    Improvement Mod menu (training, palettes, performance, dev, config)
    F2    Hitboxes (training only)
    F5    Pause / resume (training only)
    F6    Next frame while paused

Palettes are .impl files in mods\BBTAG-Improvement-Mod\palettes\<Character>\.

A mod folder can hold data\ (replacement game files), BBTAG.exe, a .CT table
and Mod Loader plugin DLLs.

Problems? Read logs\BBTAG_IM.log and logs\ImprovementMod.log.
