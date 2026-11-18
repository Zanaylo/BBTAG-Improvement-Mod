using System.Globalization;
using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods.Archives;

public sealed class ArchiveModCache(IFileSystem files)
{
    public const string FolderName = ".archive-cache";

    private const string StampFile = ".mod-archive";

    public ModFolder WithFilesCopiedOut(ModFolder mod, string archive)
    {
        string[] inside = [.. OptionalPath(mod.ExecutablePath), .. OptionalPath(mod.TablePath), .. mod.PluginPaths];

        if (inside.Length == 0)
            return mod;

        var cache = Path.Combine(Path.GetDirectoryName(archive)!, FolderName, Path.GetFileName(archive));
        string OutOfArchive(string path) => Path.Join(cache, Path.GetRelativePath(mod.RootPath, path));

        if (!IsUpToDate(cache, archive, inside.Select(OutOfArchive)))
            Refill(cache, archive, inside.ToDictionary(path => path, OutOfArchive));

        return mod with
        {
            ExecutablePath = mod.ExecutablePath is null ? null : OutOfArchive(mod.ExecutablePath),
            TablePath = mod.TablePath is null ? null : OutOfArchive(mod.TablePath),
            PluginPaths = [.. mod.PluginPaths.Select(OutOfArchive)],
        };
    }

    private bool IsUpToDate(string cache, string archive, IEnumerable<string> copies)
    {
        var stamp = Path.Combine(cache, StampFile);
        return files.FileExists(stamp) && files.ReadAllText(stamp) == StampOf(archive) && copies.All(files.FileExists);
    }

    private void Refill(string cache, string archive, IReadOnlyDictionary<string, string> copies)
    {
        if (files.DirectoryExists(cache))
            files.DeleteDirectory(cache);

        foreach (var (inside, copy) in copies)
            files.WriteAllBytes(copy, files.ReadAllBytes(inside));

        files.WriteAllText(Path.Combine(cache, StampFile), StampOf(archive));
    }

    private string StampOf(string archive) => files.LastWriteTimeUtc(archive).Ticks.ToString(CultureInfo.InvariantCulture);

    private static string[] OptionalPath(string? path) => path is null ? [] : [path];
}
