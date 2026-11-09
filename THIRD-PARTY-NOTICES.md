# Third-party notices

The BBTAG Mod Loader and Improvement Mod are released under the
[GNU General Public License v3.0](LICENSE). They are built with the libraries below, which keep their
licenses.

## Inside the released files

| Library | Used in | License |
| --- | --- | --- |
| [.NET runtime](https://github.com/dotnet/runtime) (NativeAOT) | every `.exe` and `.dll` | MIT |
| [Avalonia](https://github.com/AvaloniaUI/Avalonia) | `BBTAG_Mod_Loader.exe` | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) and HarfBuzzSharp | `BBTAG_Mod_Loader.exe` | MIT |
| [Dear ImGui](https://github.com/ocornut/imgui) | `ImprovementMod.dll`, `dinput8.dll` | MIT |
| [cimgui](https://github.com/cimgui/cimgui) | `ImprovementMod.dll`, `dinput8.dll` | MIT |
| [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) | `ImprovementMod.dll`, `dinput8.dll` | MIT |
| [Iced](https://github.com/icedland/iced) | `BBTAG_Mod_Loader.exe` (its injected `BBTAG_IM.dll`), `ImprovementMod.dll`, `dinput8.dll` | MIT |
| [SharpCompress](https://github.com/adamhathcock/sharpcompress) | `BBTAG_Mod_Loader.exe` | MIT |

## Only for the tests

| Library | License |
| --- | --- |
| [xUnit](https://github.com/xunit/xunit) | Apache-2.0 |
| [NSubstitute](https://github.com/nsubstitute/NSubstitute) | BSD-3-Clause |
| [Bogus](https://github.com/bchavez/Bogus) | MIT |
| [coverlet](https://github.com/coverlet-coverage/coverlet) | MIT |
| [Microsoft.NET.Test.Sdk](https://github.com/microsoft/vstest) | MIT |

The full text of each license is in the repository linked above.

## Not included

No file of BlazBlue Cross Tag Battle is in this repository or in its releases. The command lists
of BBTools-py3 and the community tools are not included either: the Improvement Mod reads them from
the `bbscript` folder when the user puts them there.
