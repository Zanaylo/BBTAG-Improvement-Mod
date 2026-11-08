using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Mods;

public static class EnabledMods
{
    public static IReadOnlyList<ModFolder> Of(string modFolder, IFileSystem fileSystem)
    {
        var modsFolder = ModsFolderOf(modFolder);
        return LauncherOf(modFolder, fileSystem).EnabledOf(ModDiscovery.InModsDirectory(modsFolder, fileSystem));
    }

    public static bool IsOn(string modName, string modFolder, IFileSystem fileSystem) => LauncherOf(modFolder, fileSystem).IsModEnabled(modName);

    public static string GameFolderOf(string modFolder)
    {
        var modsFolder = ModsFolderOf(modFolder);
        return Path.GetDirectoryName(modsFolder) ?? modsFolder;
    }

    private static LauncherSettings LauncherOf(string modFolder, IFileSystem fileSystem) =>
        SettingsStores.Launcher(fileSystem, GameFolderOf(modFolder)).TryLoad() ?? new LauncherSettings();

    private static string ModsFolderOf(string modFolder) =>
        Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(modFolder)) ?? modFolder;
}
