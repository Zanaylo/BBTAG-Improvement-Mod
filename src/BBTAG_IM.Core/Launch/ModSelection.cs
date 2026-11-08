using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Launch;

public static class ModSelection
{
    public const string ModsFolderName = "mods";

    public static IReadOnlyList<ModFolder> Resolve(LaunchOptions options, string modsDirectory, IFileSystem fileSystem)
    {
        if (options.ModFolders.Count > 0)
            return options.ModFolders.Select(folder => ModFolder.Inspect(folder, fileSystem)).ToArray();

        var discovered = ModDiscovery.InModsDirectory(modsDirectory, fileSystem);

        if (discovered.Count == 0)
            throw new InvalidModFolderException(
                $"No mods to load. Put mod folders or archives inside {modsDirectory}, or drag one onto the Mod Loader.");

        return discovered;
    }
}
