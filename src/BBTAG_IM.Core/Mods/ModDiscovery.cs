using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Archives;

namespace BBTAG_IM.Core.Mods;

public static class ModDiscovery
{
    public static IReadOnlyList<ModFolder> InModsDirectory(string modsDirectory, IFileSystem fileSystem)
    {
        if (!fileSystem.DirectoryExists(modsDirectory))
            return [];

        return fileSystem.EnumerateDirectories(modsDirectory)
            .Where(directory => !IsBeingUnpacked(directory))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(directory => ModFolder.TryInspect(directory, fileSystem))
            .OfType<ModFolder>()
            .ToArray();
    }

    private static bool IsBeingUnpacked(string directory) =>
        Path.GetFileName(directory).StartsWith(ModArchiveInstaller.UnpackingPrefix, StringComparison.Ordinal);
}
