using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Archives;

namespace BBTAG_IM.Core.Mods;

public static class ModDiscovery
{
    private const char HiddenPrefix = '.';
    private const string UnpackedArchiveStamp = ".mod-archive";

    public static IReadOnlyList<ModFolder> InModsDirectory(string modsDirectory, IFileSystem fileSystem)
    {
        if (!fileSystem.DirectoryExists(modsDirectory))
            return [];

        var archives = fileSystem.EnumerateFiles(modsDirectory).Where(ModArchiveName.IsArchive).ToArray();
        var folders = fileSystem.EnumerateDirectories(modsDirectory)
            .Where(folder => !IsHidden(folder) && !WasUnpackedFromAnArchive(folder, archives, fileSystem));

        return folders.Concat(archives)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(path => ModOpener.TryOpen(path, fileSystem))
            .OfType<ModFolder>()
            .ToArray();
    }

    private static bool IsHidden(string folder) => Path.GetFileName(folder).StartsWith(HiddenPrefix);

    private static bool WasUnpackedFromAnArchive(string folder, IReadOnlyList<string> archives, IFileSystem fileSystem) =>
        fileSystem.FileExists(Path.Combine(folder, UnpackedArchiveStamp))
        && archives.Any(archive => ModArchiveName.FolderOf(archive).Equals(Path.GetFileName(folder), StringComparison.OrdinalIgnoreCase));
}
