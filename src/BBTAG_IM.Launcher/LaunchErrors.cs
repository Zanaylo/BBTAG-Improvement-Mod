using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.CheatEngine;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tables;
using BBTAG_IM.Launcher.Injection;

namespace BBTAG_IM.Launcher;

internal static class LaunchErrors
{
    public static bool IsExpected(Exception exception) =>
        exception is LaunchOptionsException or GameNotFoundException or InvalidModFolderException or LaunchFailedException
            or InvalidCheatTableException or InvalidPeImageException or BridgeException or InvalidSessionTableException or ModArchiveException or IOException;

    public static bool IsSettingsWriteError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;
}
