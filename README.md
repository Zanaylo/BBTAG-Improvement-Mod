# BBTAG Improvement Mod

Loader and Improvement Mod for **BlazBlue Cross Tag Battle** (Steam, `BBTAG.exe`, 32-bit)
written in C# / .NET 10. The launcher starts the game with mods layered from their own
folders, without replacing or editing any file in the game folder. The Improvement Mod is one
of those mods: a folder inside `mods` with a plugin DLL.

The previous [BBTAG-Improvement-Mod](https://github.com/GrimFlash/BBTAG-Improvement-Mod) was
used as reference only; nothing is built on top of it.

## How it works

`BBTAG_IM_Launcher.exe` (NativeAOT, x86, [Avalonia](https://avaloniaui.net) window)

1. Finds the game (`--game`, the launcher folder, or the Steam libraries) and the mods
   (folders passed on the command line, or every folder in `mods`).
2. Picks the executable. A mod `BBTAG.exe` built for a fixed base (relocations stripped, like
   BBTAG-OverHaul-Patches) runs as it is from the mod folder, with the game folder as working
   directory. Otherwise the installed `BBTAG.exe` runs and the payload patches it in memory.
3. Starts the executable suspended, writes `jmp $` over its entry point and lets Windows finish
   loading the process, so every DLL is in place but no game code has run.
4. Injects `BBTAG_IM.dll` with `LoadLibraryW` and calls its `BBTAG_IM_Start` export with the
   session (game folder, log path, mod folders).
5. Restores the entry point and lets the game run, or closes it if the payload failed.
6. When a mod holds a Cheat Engine table, waits for the game window, writes a session copy of
   the table with a Lua bridge appended to its script, opens a named pipe (`BBTAG_IM_<pid>`)
   and starts Cheat Engine with the copy, as admin by default. The bridge gives
   `formVariables` a default of 0 (address expressions in the table read it before any input
   form has created it), attaches to the game and connects to the pipe. The window lists the
   table's scripts and turns them on and off through it. A top level script whose name starts
   with `Core` is always on, because the overhaul crashes without it.
7. When Cheat Engine dies within two minutes of attaching, the launcher closes the game and
   starts both again, up to three tries.

The window has four tabs: **Mods** (load order, what each mod holds and a checkbox to leave it
out; turned off mods are saved in `BBTAG_IM.json` under `DisabledMods`), **Improvement Mod**
(its options, saved in `ImprovementMod.json` inside its folder), **Palettes** (a custom palette
per character color, with a preview of its colors) and **Cheat Engine** (the table's scripts and
whether the Cheat Engine window is shown).

`BBTAG_IM.dll` (NativeAOT, x86)

- **Executable patches.** When the installed `BBTAG.exe` runs and a mod ships a relocatable
  modded one, the two are compared and the differences are written into the running image.
  The game uses ASLR and Windows only reserves memory on 64 KB boundaries, so the payload
  relocates the patches itself: addresses covered by the game's relocation table, new absolute
  addresses found by decoding the patched code with [Iced](https://github.com/icedland/iced),
  and `rel32` jumps into and out of sections the mod adds.
- **Loose files.** `CreateFileA/W`, `GetFileAttributesExW` and `FindFirstFileA/ExA` are hooked
  in the game's import table. Read-only opens of a game file that a mod provides go to the
  mod's copy instead. `data` and `data2` inside a mod both mount over the game's `data`.
  When the game runs from a mod folder, paths it builds next to its own executable are sent
  to the game folder unless the mod provides the file, so saves and stock data still work.
- **Plugins.** Every `.dll` in a mod folder that exports `BBTAG_IM_ModStart` is loaded before
  the game code runs. The export is `uint __stdcall BBTAG_IM_ModStart(wchar_t* modFolder,
  wchar_t* logFolder)` and returns 0 when it started. DLLs without the export are left alone.

`ImprovementMod.dll` (NativeAOT, x86, plugin in `mods\BBTAG-Improvement-Mod`)

- **Frame limiter.** Off by default. When `FrameLimiter` is on, it hooks `Direct3DCreate9Ex` in
  the game's import table, then `CreateDevice(Ex)`, `Reset(Ex)` and `Present(Ex)` in the
  Direct3D 9 tables. V-Sync is turned off in every present parameter block and each frame is
  held to a 60 fps grid (high resolution waitable timer, then a short spin).
- **Game objects.** No game code is patched for the features below. At start the plugin finds
  four lazily built singletons (game state, battle, character list, camera list) by following
  the `call` at a unique byte pattern into its getter and taking the first `mov reg, imm32`. The
  patterns match the Steam executable and BBTAG-OverHaul-Patches alike. Every pointer is checked
  with `VirtualQuery` before it is read or written.
- **Palettes.** When a match starts, the colors of each character (the 8 palette files of its
  color in the battle's palette table) are read and kept as the game colors. The palette chosen
  for that color (`palettes\<Character>\**\*.impl`) is put on top. Every change, from a file, the
  palette editor's color picker or the blink of a picked entry (magenta and cyan for 1.5 s, every
  other color dimmed to half its luminance), is
  written over that color and a
  partner color (the one before it, or the second one for the first color), both copies, and
  the color index flips between the two at most once a frame so the game redraws it. The index
  is put back when the match ends. Colors left in memory by a previous match are recognised and
  the game colors are kept. Characters past the game's own 53 are named from the game's
  character roster: the code of its record (`mko`) names the palette folder, read through the
  roster getter's `mov eax, imm32` at run time because the overhaul moves the roster.
- **Hitbox viewer.** During training matches, the hurtboxes and hitboxes of the characters and
  the entities they own are projected with the main camera's view and projection matrices and
  drawn as filled quads with a border, plus a cross at each object's origin, on the back buffer
  inside a saved Direct3D state block, right before `Present`. Each box type has its own on/off,
  fill and outline alpha. F2 toggles it.
- **Pause and next frame.** The pattern of the game's "should the match update" check is found
  and its `xor eax, eax; ret` ending is replaced by a jump to a small block that counts the
  calls and returns a hold flag. The plugin sets the flag each frame while training is paused
  and lets exactly one update through for Next frame. Outside training matches it is cleared.
  F5 pauses and F6 plays the next frame. Holding F6 (or the Next frame button) for 20 frames
  keeps stepping, one frame every 4.
- **In-game menu.** F1 opens a [Dear ImGui](https://github.com/ocornut/imgui) window (through
  [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET), `cimgui.dll` ships in the mod folder) with
  the Training, Palettes, Performance and Config sections, laid out like UNI2-IM. The palette
  editor and the palette assignments window (BBCF-IM style: every palette per character drawn
  on its standing sprite, the in-game color that loads it, Random choices, search, import, export and
  delete) are separate windows. Every window can be resized, and ImGui keeps their positions
  and sizes in `menu-layout.ini` in the mod folder. The sprite is read at run time from the
  game's `data\char\char_<code>_img.pac` (enabled mods first): the FPAC file list, the
  `<prefix>000_00.hip` of the most common sprite prefix, its (index, run) pairs, cropped to
  the parts larger than a twentieth of the biggest one, then painted with the palette's
  character colors into a cached texture. Nothing from the game ships with the mod.
- **Character select colors.** Character select reloads `data\etc\char_select_pal\char_select_<code>_pal.pac`
  (one `<code>NN_00.hpl` per color, the same bytes as the battle colors) every time the screen
  is built. The Improvement Mod chains its own `CreateFileA`/`CreateFileW` import hook on top of
  the payload's and, for a read of that file, hands the game a copy in `cache\` with each
  assigned palette's character colors written over its color's entry. The game window procedure is subclassed for F1, F2, F5 and F6
  and, while the menu is open, for the mouse, and for the keyboard while a text field has
  focus. ImGui's draw lists are drawn with `DrawIndexedPrimitiveUP` from pretransformed
  vertices, using a dynamic font texture that is released before `Reset` and rebuilt when the
  font size changes.
- Options are reloaded about once a second, so the launcher and the in-game menu can change
  palettes and the hitbox viewer while the game runs. The launcher rereads the file too and shows
  choices made in the game.

The bridge protocol is a 4 byte little endian length followed by ASCII text: `enable <id>`,
`disable <id>`, `show`, `hide` and `quit` answer `ok` (or `missing`), `state` answers the
active script ids separated by commas and `busy` the number of scripts still turning on.
Launcher choices are saved in `BBTAG_IM.json` next to the launcher.

## Layout

| Path | Purpose |
| --- | --- |
| `src/BBTAG_IM.Core` | PE parsing, patch planning and relocation, mod folders, path redirection, launch options, settings, frame pacing, game object lookup, palettes, hitbox geometry. All unit tested. |
| `src/BBTAG_IM.Native` | Win32 helpers shared by the DLLs: import and virtual table hooks, protected memory writes, checked process memory access. |
| `src/BBTAG_IM.Payload` | The injected DLL: file hooks, in-memory patching, plugin loading. |
| `src/BBTAG_IM.ImprovementMod` | The Improvement Mod plugin. |
| `src/BBTAG_IM.Launcher` | The launcher: window (`Ui/*.axaml`), process creation, injection, Cheat Engine host. |
| `tests/BBTAG_IM.Core.Tests` | xUnit + NSubstitute + Bogus, Arrange / Act / Assert. |
| `resource/README.txt` | User readme shipped with the build. |

The launcher window is XAML, so the Avalonia previewer (Visual Studio or Rider extension) shows
it with the sample data in `Ui/LauncherDesignData.cs`.

## Building

Needs the .NET 10 SDK and Visual Studio with the C++ x86 tools (NativeAOT links with MSVC).

```powershell
./build.ps1
```

Runs the tests and publishes `BBTAG_IM_Launcher.exe` with the Avalonia native DLLs
(`libSkiaSharp.dll`, `libHarfBuzzSharp.dll`, `av_libglesv2.dll`), `BBTAG_IM.dll`, `README.txt`
and `mods\BBTAG-Improvement-Mod\ImprovementMod.dll` (with an empty `palettes` folder) to
`export/BBTAG_IM`.
