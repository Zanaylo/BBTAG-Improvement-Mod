BBTAG Mod Loader
================

Starts BlazBlue Cross Tag Battle with mods loaded from separate folders.
The game folder is never changed: to uninstall, delete BBTAG_Mod_Loader.exe,
this README and the "cache" and "mods" folders.

Installing
----------
Copy every file and the "mods" folder next to BBTAG.exe
(Steam: right click the game > Manage > Browse local files).
The Mod Loader makes a "cache" folder there for its files when it starts.
Older versions had BBTAG_IM.dll, libSkiaSharp.dll, libHarfBuzzSharp.dll and
av_libglesv2.dll next to it. They are not used anymore and can be deleted.

Using it
--------
Start Steam, run BBTAG_Mod_Loader.exe and press Play.

Mods tab: tick the mods to load and use the arrows to set the order. When two
mods replace the same file, the higher one wins. Keep BBTAG-Improvement-Mod
above BBTAG-OverHaul.

When a ticked mod has a Cheat Engine table with a character roster, the
message list names every character that lacks files a match needs.

A mod can add up to 8 characters to the native BBTAG.exe. Put one file per
character in a "characters" folder of the mod, like characters\rar.json with
{ "Code": "rar", "Template": "brg", "Series": "Others" }. The character plays
like its template. Series is the row on character select: BlazBlue, Persona,
UnderNight, Rwby or Others.

An error during a match no longer closes the game. The match goes back to
character select, and the error is written in logs\ImprovementMod.log and shown
in the Mod Loader message list.

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
