# BBTAG Mod Loader and Improvement Mod

Mod Loader and Improvement Mod for **BlazBlue Cross Tag Battle** (Steam, `BBTAG.exe`, 32-bit),
written in C# / .NET 10.

- **Mod Loader** (`BBTAG_Mod_Loader.exe`): starts the game with mods layered from separate folders
  in `mods`, without changing any file in the game folder. Runs a mod's Cheat Engine table when it
  has one. A mod can be linked to its GitHub repository and is then kept up to date.
- **Improvement Mod** (`mods\BBTAG-Improvement-Mod`, or `dinput8.dll` alone in the player
  edition): the F1 menu in the game. Hitbox viewer, pause and frame stepping, frame meter, palettes
  and a palette editor, boss fights, V-Sync off at 60 fps, twelve window resolutions, and live
  script editing with rollback when an edit breaks the game.
- **Updater** (`BBTAG_IM_Updater.exe`): installs new versions of the Improvement Mod from the
  GitHub releases of this repository.


## Installing

Download a zip from the [releases](https://github.com/Zanaylo/BBTAG-Improvement-Mod/releases) and
unpack it next to `BBTAG.exe` (Steam: right click the game > Manage > Browse local files).

| Zip | Holds | Starts with |
| --- | --- | --- |
| `BBTAG_IM-<version>.zip` | `dinput8.dll` and the updater | `BBTAG.exe`, as usual |
| `BBTAG_IM-<version>-dev.zip` | Mod Loader, `ImprovementMod.dll` and the updater. Dev tab, boss fights and mod folders included | `BBTAG_Mod_Loader.exe` |

## Updates

The player edition starts the updater when the game starts, and the Mod Loader starts it when it
opens. The updater checks the newest release quietly. When there is a newer version, it waits for
the game or the Mod Loader to close and asks before installing. Running `BBTAG_IM_Updater.exe` by
hand checks right away.

It picks the zip of the installed edition: the one ending in `-dev.zip` when
`BBTAG_Mod_Loader.exe` is in the folder, otherwise the other `.zip`. The release tag holds the
version (`v0.2.0`), compared with the version of `BBTAG_Mod_Loader.exe` or `dinput8.dll`.

## Mods from GitHub

In the Mod Loader's Mods tab, paste a repository link and save it on a mod, or add it as a new mod.
The link is kept in `.mod-source.json` inside the mod folder. Linked mods are checked when the Mod
Loader opens (an option in the Config tab) or with Update linked mods.

- When the newest release of the repository has a `.zip`, `.rar` or `.7z`, that archive is
  installed.
- Otherwise the mod follows the newest commit of the main branch. Only the files that changed are
  downloaded, each one checked against its Git hash, and files the repository removed are deleted.
  Files that never came from the repository stay.

## How it works

The Mod Loader starts the game suspended, injects `BBTAG_IM.dll` and lets the game run. The payload
hooks the game's file functions so mod files are read instead of the game's, applies a mod's
executable patches in memory, and loads every mod DLL that exports `BBTAG_IM_ModStart`.
`ImprovementMod.dll` is one of those: it finds the game objects by byte patterns and draws its
menu inside the game's Direct3D 9 frame.

The player edition has no Mod Loader. `dinput8.dll` goes next to `BBTAG.exe`, which loads it by
itself. It forwards `DirectInput8Create` to the system library, holds the game at its entry point
while the mod starts (`Standalone/entry_gate.c`, the one piece that has to run at load time), and
stays out of the way when a Mod Loader already holds the entry point. Its options, palettes and
cache live in `mods\BBTAG-Improvement-Mod`. `"WriteLogs": true` in a `BBTAG_IM.json` next to the
game turns its log on.

## Layout

| Path | Purpose |
| --- | --- |
| `src/BBTAG_IM.Core` | Everything that runs without the game. All unit tested. |
| `src/BBTAG_IM.Native` | Win32 helpers shared by the DLLs. |
| `src/BBTAG_IM.Payload` | The injected DLL. |
| `src/BBTAG_IM.ImprovementMod` | The Improvement Mod plugin. |
| `src/BBTAG_IM.Launcher` | The Mod Loader window. |
| `src/BBTAG_IM.Updater` | The updater, a Windows task dialog. |
| `src/BBTAG_IM.BBScript` | `bbscript.exe`, the script compiler behind the Dev tab. |
| `tests/BBTAG_IM.Core.Tests` | xUnit + NSubstitute + Bogus. |
| `resource/README.txt` | User readme, shipped only with `-WithTools`. |
| `resource/Updater` | The updater icon. |

## Building

Needs the .NET 10 SDK and Visual Studio with the C++ x86 tools (NativeAOT links with MSVC).

```powershell
./build.ps1
```

Runs the tests and writes both zips to `export`. The build fails if any other file lands in a zip,
or if a word that must stay hidden is found in the player `dinput8.dll`. The version comes from
`Directory.Build.props`.

`./build.ps1 -WithTools` writes a Mod Loader folder with `bbscript.exe`, its Python version and the
script editor project instead.

## Publishing a release

1. Raise `Version` in `Directory.Build.props` and run `./build.ps1`.
2. Create a GitHub release tagged `v<version>` and attach `BBTAG_IM-<version>.zip` and
   `BBTAG_IM-<version>-dev.zip`.

The repository must stay public for the updater and linked mods to reach it.

## Credits

- [BBTAG-Improvement-Mod](https://github.com/GrimFlash/BBTAG-Improvement-Mod): the previous
  Improvement Mod. Reference for the game objects, the palette format and the hitboxes. No code was
  taken from it.
- [BBTools-py3](https://github.com/MorphRed/BBTools-py3) and [bbtools](https://github.com/dantarion/bbtools):
  the BBScript format and the command lists the script editor reads.
- **Team Blue Modding (and Friends)** Discord: the BBTAG script tools
  and Mango's script parser, whose layout the Python scripts follow, and the community command lists.
- [UNI2-Improvement-Mod](https://github.com/Zanaylo/UNI2-Improvement-Mod): the menu layout.
- [Avalonia](https://avaloniaui.net), [Dear ImGui](https://github.com/ocornut/imgui),
  [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET), [Iced](https://github.com/icedland/iced),
  [SharpCompress](https://github.com/adamhathcock/sharpcompress). See
  [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Thanks to Geo, Ryn, Danny, Booty, Azazel_Deamon and firezeroboredvanguard.

## License

Released under the [GNU General Public License v3.0](LICENSE). The libraries it uses keep their
licenses, listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

BlazBlue Cross Tag Battle belongs to Arc System Works. This is a fan project, not affiliated with or
endorsed by Arc System Works. No game file is in this repository or in its releases.
