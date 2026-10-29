using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Mods;

public static class EnabledMods
{
    public static IReadOnlyList<ModFolder> Of(string modFolder, IFileSystem fileSystem)
    {
        var modsFolder = ModsFolderOf(modFolder);
        var launcher = SettingsStores.Launcher(fileSystem, GameFolderOf(modFolder)).TryLoad() ?? new LauncherSettings();
        return launcher.EnabledOf(ModDiscovery.InModsDirectory(modsFolder, fileSystem));
    }

    public static string GameFolderOf(string modFolder)
    {
        var modsFolder = ModsFolderOf(modFolder);
        return Path.GetDirectoryName(modsFolder) ?? modsFolder;
    }

    private static string ModsFolderOf(string modFolder) =>
        Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(modFolder)) ?? modFolder;
}
