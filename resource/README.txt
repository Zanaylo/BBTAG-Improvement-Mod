BBTAG Improvement Mod
=====================

Starts BlazBlue Cross Tag Battle with mods loaded from their own folders.
Nothing in the game folder is replaced or changed, so Steam's "Verify integrity"
never undoes a mod and uninstalling is deleting the launcher files.

The Improvement Mod itself is one of those mods: the BBTAG-Improvement-Mod folder
inside "mods".

Files
-----
BBTAG_IM_Launcher.exe   The launcher window. Starts the game and loads the mods.
BBTAG_IM.dll            Loaded into the game by the launcher.
*.dll                   The other DLLs draw the launcher window. Keep them all together.
mods\                   Put mod folders here.
mods\BBTAG-Improvement-Mod\
                        The Improvement Mod. Its options are in the launcher and in
                        the F1 menu in the game. cimgui.dll draws that menu.
mods\BBTAG-Improvement-Mod\palettes\
                        Custom palettes (.impl files), one folder per character.
mods\BBTAG-Improvement-Mod\menu-layout.ini
                        Where the F1 menu windows were and how big you made them.
logs\BBTAG_IM.log       Written on every start. Read it when something does not load.
logs\ImprovementMod.log Written by the Improvement Mod on every start.
BBTAG_IM.json           Your launcher choices. Written when you change one.

Installing
----------
Copy every file and the "mods" folder into the game folder, next to BBTAG.exe
(Steam: right click the game > Manage > Browse local files):

    BBTAG\
        BBTAG.exe
        BBTAG_IM_Launcher.exe
        BBTAG_IM.dll
        mods\
            BBTAG-Improvement-Mod\
                ImprovementMod.dll
                cimgui.dll
            BBTAG-OverHaul\
                BBTAG.exe
                BBTAG.CT
                data\

Keep the game's native BBTAG.exe and data folder unmodded. The launcher layers the mods
over them.

Using it
--------
Start Steam, then run BBTAG_IM_Launcher.exe and press Play. Starting the game from
Steam or from BBTAG.exe runs it without mods.

The launcher has four tabs.

Mods shows every mod in the mods folder, in load order, and what each one holds.
Untick a mod to leave it out the next time you press Play: its files, executable,
table and plugin are not used. The choice is saved in BBTAG_IM.json. The list cannot
change while the game runs.

Improvement Mod holds the options of the Improvement Mod. They are saved in
ImprovementMod.json inside its folder.

    Turn V-Sync off and hold the game at 60 fps
        Off by default. V-Sync adds input delay, but without it the game does not keep
        a steady 60 fps, which online play needs. With this on, V-Sync is off and the
        mod holds the game at 60 fps by itself. Leave the game's native V-Sync option as
        it is; the mod overrides it. Takes effect the next time the game starts.

    Show hitboxes in training mode
        On by default. During a training match, green boxes show where a character can
        be hit and red boxes show attacks, projectiles included. Other modes never
        draw them. You can tick or untick it while the game runs.

In the game, press F1 to open the Improvement Mod menu. It is used with the mouse and
has four sections, Training first. Press F1 again or close the window to hide it.
Drag a window's edges or corner to make it the size you want. Every window keeps
its place and size the next time.
Choices made there are saved in ImprovementMod.json, and the launcher shows them too.

    F2    Shows or hides the hitboxes (training only).
    F5    Pauses or resumes the match (training only).
    F6    Plays the next frame while paused. Hold it to keep playing frames slowly.

    Training
        The hitbox viewer, the same option as F2. Pause and Next frame do the same as
        F5 and F6. Under Hitbox types you can turn each kind of box on or off, set how
        see-through its fill and outline are, and show a cross at the point each
        object's boxes are measured from.

    Palettes
        Works during a match. Pick a saved palette for each character, or press Rescan
        to see palettes added while the game runs. Open editor opens the palette editor
        window.

        Palette assignments works anytime, not only in a match. It lists every
        palette of every character, drawn on the character's standing pose. The
        picture is read from the game's own files (and from enabled mods, for modded
        characters). If those files cannot be read, for example when the game data is
        still encrypted, the palette shows stripes of its colors instead. Click one
        to see it on the right,
        then pick its in-game color: picking that color in game loads the palette
        automatically. Random and Random (exclude game colors) can be given a color
        too, and each match then picks one of the character's palettes. Search finds
        palettes or characters by name. Import palettes copies .impl files into the
        right character folder. The right side also has Edit palette (during a match,
        on a character that is playing), Export .impl and Delete. Drag the line between
        the two sides to make either one wider.

        A palette assigned to a color is also shown on character select when you
        pick that color. Character select reads its colors when you enter it, so a
        change made while you are on that screen shows up the next time you enter.
        Random colors show the game colors there.

        The editor has a tab for each of the four characters. Click a color in the grid
        to pick it: for a moment every other color of the character goes dark and the
        picked one blinks, so you can see where it is used. Change it with the color picker below and the character changes on
        screen right away. The Effects tab does the same for the colors of the
        character's effects. Undo takes back the last change and Reset goes back to the
        game colors. Type a name, an author and a description, then press Save to keep
        your colors as a palette in the palettes folder. Whatever the character wears is
        remembered for that color and put on again in every match. It only changes your
        screen: your opponent still sees the normal color.

    Performance
        Turn V-Sync off, the same option as the launcher's frame limiter.

    Config
        The font size of the menu. Default makes it follow the screen height again.

The launcher's Palettes tab picks a saved palette for any of the 20 colors of a
character. Pick the character, then a palette for the color you want to replace. The
small square shows the palette colors. The swap happens when the next match starts.

Palettes are .impl files (the format of the previous Improvement Mod) inside
mods\BBTAG-Improvement-Mod\palettes, in a folder named after the character:

    palettes\
        Aegis\
            Minato.impl
        Ragna\
            Pack\
                Batman Ragna.impl
        mko\
            Red.impl

The file name is the palette name. Subfolders are fine. A palette made for another
character is skipped and the launcher says so. Folder names are the character names
used by the previous Improvement Mod (Ragna, Jin, ... NaotoKurogane, Blitztank).
Characters added by a mod, like the ones of the overhaul, use the short code the game
gives them (mko, uby, ...). The F1 menu shows that code next to their name. Save a
palette in the game once to have the folder made for you.

Cheat Engine shows the scripts of the mod's Cheat Engine table (a .CT file). Tick the
ones you want before or during the match. After the game window opens, the launcher
starts Cheat Engine as admin with the table, attaches it to the game and turns on the
ticked scripts. Accept the Windows prompt when it shows up. Stay in the menus until
the launcher says "You can start a match now". The table turns its scripts on in the
background and that can take a while. A match started before that crashes the game.
Cheat Engine closes with the game.

The Core script is always on. The overhaul needs it, so it cannot be unticked.

Untick "Show the Cheat Engine window" to keep Cheat Engine hidden while it runs. You
can tick it again during the match to bring the window back.

Cheat Engine sometimes crashes while it turns the scripts on. When that happens in the
first two minutes, the launcher closes the game and starts both again by itself, up to
three tries.

Cheat Engine 7.7 must be installed. Settings live in BBTAG_IM.json next to the launcher:

    {
      "CheatEnginePath": "D:\\Tools\\Cheat Engine",
      "RunCheatEngineAsAdmin": true,
      "ShowCheatEngineWindow": true
    }

CheatEnginePath is only needed when Cheat Engine is not in Program Files. Set
RunCheatEngineAsAdmin to false to skip the Windows prompt.

If Cheat Engine asks whether to run the table's Lua script, answer Yes. Cheat Engine's
settings can make it always run table scripts, so it stops asking.

You can also drag one or more mod folders onto BBTAG_IM_Launcher.exe. Folders given
that way are loaded instead of the "mods" folder. The launcher can also live outside
the game folder: it then finds the game through Steam, or through --game "<folder>".

What a mod folder holds
-----------------------
data\ or data2\   Files laid out like the game's "data" folder. A file here is used
                  instead of the game's file with the same path, and new files are
                  added.
BBTAG.exe         Optional. A modded copy of the game executable. When it was built to
                  load at a fixed address (BBTAG-OverHaul-Patches is), the game runs
                  from it. Otherwise its differences from your installed BBTAG.exe are
                  applied in memory. Either way it must come from the same game version
                  as your installed copy.
*.CT              Optional. A Cheat Engine table whose scripts the launcher can turn on.
                  BBTAG.CT is picked first when there are several.
*.dll             Optional. A plugin loaded into the game before it starts. Only DLLs
                  made for the launcher are loaded; any other DLL is left alone.

When two mods replace the same file, the one that comes first wins: name order inside
"mods", or the order you passed the folders in.
