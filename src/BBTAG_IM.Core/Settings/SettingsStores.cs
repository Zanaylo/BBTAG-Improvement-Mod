using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Settings;

public static class SettingsStores
{
    public static JsonFileStore<LauncherSettings> Launcher(IFileSystem fileSystem, string launcherDirectory) =>
        new(fileSystem, Path.Combine(launcherDirectory, LauncherSettings.FileName), SettingsJson.Default.LauncherSettings);

    public static JsonFileStore<ImprovementSettings> Improvement(IFileSystem fileSystem, string modDirectory) =>
        new(fileSystem, Path.Combine(modDirectory, ImprovementSettings.FileName), SettingsJson.Default.ImprovementSettings);

    public static JsonFileStore<UpdaterSettings> Updater(IFileSystem fileSystem, string gameDirectory) =>
        new(fileSystem, Path.Combine(gameDirectory, ModSelection.ModsFolderName, ImprovementModContract.ModFolderName, UpdaterSettings.FileName), SettingsJson.Default.UpdaterSettings);
}
