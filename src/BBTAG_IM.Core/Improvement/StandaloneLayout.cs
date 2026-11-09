using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Improvement;

public sealed record StandaloneLayout(string GameFolder, string ModFolder, string LogFolder)
{
    private const string NoLogs = "";

    public static StandaloneLayout For(string gamePath, IFileSystem fileSystem)
    {
        var gameFolder = Path.GetDirectoryName(gamePath) ?? gamePath;
        var modFolder = Path.Combine(gameFolder, ModSelection.ModsFolderName, ImprovementModContract.ModFolderName);
        var writeLogs = SettingsStores.Launcher(fileSystem, gameFolder).TryLoad()?.WriteLogs ?? false;

        return new StandaloneLayout(gameFolder, modFolder, writeLogs ? Path.Combine(gameFolder, PayloadContract.LogFolder) : NoLogs);
    }
}
