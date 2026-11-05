# BBTAG Mod Loader and Improvement Mod

Mod Loader and Improvement Mod for **BlazBlue Cross Tag Battle** (Steam, `BBTAG.exe`, 32-bit),
written in C# / .NET 10.

- **Mod Loader** (`BBTAG_Mod_Loader.exe`): starts the game with mods layered from separate folders
  in `mods`, without changing any file in the game folder. Runs a mod's Cheat Engine table when it
  has one.
- **Improvement Mod** (`mods\BBTAG-Improvement-Mod`): the F1 menu in the game. Hitbox viewer, pause
  and frame stepping, palettes and a palette editor, boss fights, V-Sync off at 60 fps, twelve
  window resolutions, and live script editing with rollback when an edit breaks the game.

The previous [BBTAG-Improvement-Mod](https://github.com/GrimFlash/BBTAG-Improvement-Mod) was
used as reference only; nothing is built on top of it.

## How it works

The Mod Loader starts the game suspended, injects `BBTAG_IM.dll` and lets the game run. The payload
hooks the game's file functions so mod files are read instead of the game's, applies a mod's
executable patches in memory, and loads every mod DLL that exports `BBTAG_IM_ModStart`.
`ImprovementMod.dll` is one of those: it finds the game objects by byte patterns and draws its
menu inside the game's Direct3D 9 frame.

## Layout

| Path | Purpose |
| --- | --- |
| `src/BBTAG_IM.Core` | Everything that runs without the game. All unit tested. |
| `src/BBTAG_IM.Native` | Win32 helpers shared by the DLLs. |
| `src/BBTAG_IM.Payload` | The injected DLL. |
| `src/BBTAG_IM.ImprovementMod` | The Improvement Mod plugin. |
| `src/BBTAG_IM.Launcher` | The Mod Loader window. |
| `tests/BBTAG_IM.Core.Tests` | xUnit + NSubstitute + Bogus. |
| `resource/README.txt` | User readme shipped with the build. |

## Building

Needs the .NET 10 SDK and Visual Studio with the C++ x86 tools (NativeAOT links with MSVC).

```powershell
./build.ps1
```

Runs the tests and publishes everything to `export/BBTAG_IM`.
