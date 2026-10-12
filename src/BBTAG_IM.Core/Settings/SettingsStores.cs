using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Settings;

public static class SettingsStores
{
    public static JsonFileStore<LauncherSettings> Launcher(IFileSystem fileSystem, string launcherDirectory) =>
        new(fileSystem, Path.Combine(launcherDirectory, LauncherSettings.FileName), SettingsJson.Default.LauncherSettings);

    public static JsonFileStore<ImprovementSettings> Improvement(IFileSystem fileSystem, string modDirectory) =>
        new(fileSystem, Path.Combine(modDirectory, ImprovementSettings.FileName), SettingsJson.Default.ImprovementSettings);
}
