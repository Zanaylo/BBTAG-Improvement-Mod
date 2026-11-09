BBTAG Mod Loader
================

Starts BlazBlue Cross Tag Battle with mods loaded from separate folders.
The game folder is never touched, to uninstall: delete BBTAG_Mod_Loader.exe,
BBTAG_IM_Updater.exe, this README and the "cache" and "mods" folders.

Installing
----------
Copy every file and the "mods" folder next to BBTAG.exe
(Steam: right click the game > Manage > Browse local files).
The Mod Loader makes a "cache" folder there for its files when it starts.

Using it
--------
Start Steam, run BBTAG_Mod_Loader.exe and press Play.

Mods tab: tick the mods to load and use the arrows to set the order. When two
mods replace the same file, the higher one wins. Keep BBTAG-Improvement-Mod
above BBTAG-OverHaul.

A mod can also be a .zip, .rar, .7z, .tar, .tar.gz or .tgz file. Put it in the
mods folder (or drag it onto BBTAG_Mod_Loader.exe) and it is unpacked into a
folder of the same name there, the next time the Mod Loader starts or you press
Refresh. A newer archive updates that folder. Files you added to it stay, and so
does a folder that did not come from an archive. Deleting the archive does not
delete the folder.

GitHub links: paste the link of a mod's GitHub repository in the box under
the list. "Save on the selected mod" keeps that mod up to date, "Add as a new
mod" downloads it into a new folder. Linked mods download their new files when
the Mod Loader opens, or when you press Update linked mods. Only the files
that changed are downloaded, and files the repository removed are deleted.
Files you added yourself stay. Leave the box empty and save to remove a link.
The GitHub button on a linked mod opens its page. BBTAG-Improvement-Mod
always links to its repository and is updated by BBTAG_IM_Updater.exe.

Cheat Engine tab: tick the table scripts you want. Needs Cheat Engine 7.7.

Config tab: Write log files is off by default. Tick it to save logs in the
logs folder and to list errors from inside the game in the Mod Loader.
Press Play when the Mod Loader opens starts the game with the ticked mods, once
each time the Mod Loader opens. Close the game to change the mods.
Update linked mods when the Mod Loader opens is on by default.

Updates
-------
BBTAG_IM_Updater.exe looks for a new version of the Improvement Mod each time
the Mod Loader opens. When there is one, it asks after you close the Mod
Loader. Run it by hand to check right away.

In the game
-----------
    F1    Improvement Mod menu (training, palettes, performance, dev, config)
    F2    Hitboxes (training only)
    F3    Frame meter (training only)
    F5    Pause / resume (training only)
    F6    Next frame while paused

Palettes are .impl files in mods\BBTAG-Improvement-Mod\palettes\<Character>\.

A mod folder can hold data\ (replacement game files), BBTAG.exe, a .CT table
and Mod Loader plugin DLLs.

Problems? Tick Write log files in the Config tab, do it again, and send
logs\BBTAG_IM.log and logs\ImprovementMod.log.
