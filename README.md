# BBTAG Improvement Mod

Loader for **BlazBlue Cross Tag Battle** (Steam, `BBTAG.exe`, 32-bit) written in C# / .NET 10.
It starts the game with mods layered from their own folders, without replacing or editing any
file in the game folder.

The previous [BBTAG-Improvement-Mod](https://github.com/GrimFlash/BBTAG-Improvement-Mod) was
used as reference only; nothing is built on top of it.

## How it works

`BBTAG_IM_Launcher.exe` (NativeAOT, x86)

1. Finds the game (`--game`, the launcher folder, or the Steam libraries) and the mods
   (folders passed on the command line, or every folder in `mods`).
2. Starts `BBTAG.exe` suspended, writes `jmp $` over its entry point and lets Windows finish
   loading the process, so every DLL is in place but no game code has run.
3. Injects `BBTAG_IM.dll` with `LoadLibraryW` and calls its `BBTAG_IM_Start` export with the
   session (game folder, log path, mod folders).
4. Restores the entry point and lets the game run, or closes it if the payload failed.

`BBTAG_IM.dll` (NativeAOT, x86)

- **Executable patches.** When a mod ships a modded `BBTAG.exe`, it is compared with the
  installed one and the differences are written into the running image. The game uses ASLR
  and Windows only reserves memory on 64 KB boundaries, so the payload relocates the patches
  itself: addresses covered by the game's relocation table, new absolute addresses found by
  decoding the patched code with [Iced](https://github.com/icedland/iced), and `rel32` jumps
  into and out of sections the mod adds (`.extend` in BBTAG-OverHaul-Patches).
- **Loose files.** `CreateFileA/W`, `GetFileAttributesExW` and `FindFirstFileA/ExA` are hooked
  in the game's import table. Read-only opens of a game file that a mod provides go to the
  mod's copy instead. `data` and `data2` inside a mod both mount over the game's `data`.

## Layout

| Path | Purpose |
| --- | --- |
| `src/BBTAG_IM.Core` | PE parsing, patch planning and relocation, mod folders, path redirection, launch options. All unit tested. |
| `src/BBTAG_IM.Payload` | The injected DLL: import hooks and in-memory patching. |
| `src/BBTAG_IM.Launcher` | The launcher: process creation and injection. |
| `tests/BBTAG_IM.Core.Tests` | xUnit + NSubstitute + Bogus. |
| `resource/README.txt` | User readme shipped with the build. |

## Building

Needs the .NET 10 SDK and Visual Studio with the C++ x86 tools (NativeAOT links with MSVC).

```powershell
./build.ps1
```

Runs the tests and publishes `BBTAG_IM_Launcher.exe`, `BBTAG_IM.dll`, `README.txt` and an empty
`mods` folder to `export/BBTAG_IM`.
