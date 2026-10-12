using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods;

public static class ModDiscovery
{
    public static IReadOnlyList<ModFolder> InModsDirectory(string modsDirectory, IFileSystem fileSystem)
    {
        if (!fileSystem.DirectoryExists(modsDirectory))
            return [];

        return fileSystem.EnumerateDirectories(modsDirectory)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(directory => TryInspect(directory, fileSystem))
            .OfType<ModFolder>()
            .ToArray();
    }

    private static ModFolder? TryInspect(string directory, IFileSystem fileSystem)
    {
        try
        {
            return ModFolder.Inspect(directory, fileSystem);
        }
        catch (InvalidModFolderException)
        {
            return null;
        }
    }
}
