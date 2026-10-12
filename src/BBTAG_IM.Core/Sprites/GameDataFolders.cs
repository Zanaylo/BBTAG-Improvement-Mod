using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Sprites;

public static class GameDataFolders
{
    public static IReadOnlyList<string> Of(string modFolder, IFileSystem fileSystem)
    {
        var modsFolder = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(modFolder)) ?? modFolder;
        var gameFolder = Path.GetDirectoryName(modsFolder) ?? modsFolder;
        var launcher = SettingsStores.Launcher(fileSystem, gameFolder).TryLoad() ?? new LauncherSettings();
        var mods = launcher.EnabledOf(ModDiscovery.InModsDirectory(modsFolder, fileSystem));
        var modData = mods.SelectMany(mod => mod.DataRoots).Select(root => root.Path);

        return [.. modData, Path.Combine(gameFolder, ModFolder.GameDataFolder)];
    }
}
