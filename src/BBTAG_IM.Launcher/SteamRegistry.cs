using BBTAG_IM.Core.Steam;
using Microsoft.Win32;

namespace BBTAG_IM.Launcher;

internal sealed class SteamRegistry : ISteamInstallation
{
    private const string SteamKey = @"HKEY_CURRENT_USER\Software\Valve\Steam";
    private const string SteamPathValue = "SteamPath";

    public string? SteamPath =>
        (Registry.GetValue(SteamKey, SteamPathValue, null) as string)?.Replace('/', Path.DirectorySeparatorChar);
}
