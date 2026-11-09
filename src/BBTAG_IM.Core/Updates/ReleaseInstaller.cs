using System.IO.Compression;
using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods.Archives;

namespace BBTAG_IM.Core.Updates;

public sealed class ReleaseInstaller
{
    public const string LeftoverSuffix = ".replaced";

    private const string NewSuffix = ".new";

    public void Install(string zipPath, string folder)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var files = zip.Entries.Where(IsFile).Select(entry => (Entry: entry, Path: ArchiveEntryPath.Inside(folder, entry.FullName))).ToArray();

        foreach (var (entry, path) in files)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            entry.ExtractToFile(path + NewSuffix, overwrite: true);
        }

        foreach (var (_, path) in files)
            Replace(path);

        RemoveLeftovers(folder);
    }

    public void RemoveLeftovers(string folder)
    {
        foreach (var place in LeftoverPlaces(folder).Where(Directory.Exists))
        {
            foreach (var leftover in Directory.EnumerateFiles(place, "*" + LeftoverSuffix))
                TryDelete(leftover);
        }
    }

    private static void Replace(string path)
    {
        if (File.Exists(path))
            File.Move(path, path + LeftoverSuffix, overwrite: true);

        File.Move(path + NewSuffix, path);
    }

    private static IEnumerable<string> LeftoverPlaces(string folder) =>
        [folder, Path.Combine(folder, ModSelection.ModsFolderName, ImprovementModContract.ModFolderName)];

    private static bool IsFile(ZipArchiveEntry entry) => entry.Name.Length > 0;

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
