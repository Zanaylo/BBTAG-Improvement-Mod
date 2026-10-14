using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tables;

namespace BBTAG_IM.Launcher;

internal sealed record LaunchContext(
    string LauncherDirectory,
    string GameDirectory,
    IReadOnlyList<ModFolder> Mods,
    LaunchTarget Target,
    string? TablePath,
    CheatTable? Table)
{
    public string LogPath => Path.Combine(LauncherDirectory, PayloadContract.LogFolder, PayloadContract.LogFileName);

    public string TableName => Path.GetFileName(TablePath) ?? "";

    public static LaunchContext Resolve(LaunchOptions options, string launcherDirectory, IReadOnlyList<ModFolder> mods)
    {
        var fileSystem = new PhysicalFileSystem();
        var gameDirectory = new GameLocator(fileSystem, new SteamRegistry()).Locate(options.GameDirectory, launcherDirectory);
        var target = LaunchTarget.Choose(gameDirectory, mods, path => FileImage.Load(path).Headers);
        var tablePath = mods.Select(mod => mod.TablePath).FirstOrDefault(path => path is not null);
        var table = tablePath is null ? null : CheatTable.Parse(fileSystem.ReadAllText(tablePath));

        return new LaunchContext(launcherDirectory, gameDirectory, mods, target, tablePath, table);
    }
}
