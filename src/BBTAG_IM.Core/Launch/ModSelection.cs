using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Launch;

public static class ModSelection
{
    public const string ModsFolderName = "mods";

    public static IReadOnlyList<ModFolder> Resolve(LaunchOptions options, string launcherDirectory, IFileSystem fileSystem)
    {
        if (options.ModFolders.Count > 0)
            return options.ModFolders.Select(folder => ModFolder.Inspect(folder, fileSystem)).ToArray();

        var modsDirectory = Path.Combine(launcherDirectory, ModsFolderName);
        var discovered = ModDiscovery.InModsDirectory(modsDirectory, fileSystem);

        if (discovered.Count == 0)
            throw new InvalidModFolderException(
                $"No mods to load. Put mod folders inside {modsDirectory}, or drag a mod folder onto the launcher.");

        return discovered;
    }
}
