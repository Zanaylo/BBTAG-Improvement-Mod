using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods;

public sealed class DevOverlay(IFileSystem files, string devFolder, Func<string, string?> original)
{
    public const string CacheFolder = ".cache";

    private const string ArchiveExtension = ".pac";
    private const string SourceExtension = ".py";

    private readonly Lock _building = new();

    public string? Resolve(string key)
    {
        var loose = Path.Join(devFolder, key);

        if (files.FileExists(loose))
            return loose;

        if (!key.EndsWith(ArchiveExtension, StringComparison.OrdinalIgnoreCase))
            return null;

        var folder = loose[..^ArchiveExtension.Length];

        if (!files.DirectoryExists(folder))
            return null;

        var looseFiles = files.EnumerateFiles(folder)
            .Where(file => !file.EndsWith(SourceExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return looseFiles.Count == 0 ? null : Built(key, looseFiles);
    }

    private string Built(string key, IReadOnlyList<string> looseFiles)
    {
        var cache = Path.Join(devFolder, CacheFolder, key);
        var source = original(key);

        lock (_building)
        {
            if (IsUpToDate(cache, source is null ? looseFiles : [.. looseFiles, source]))
                return cache;

            var archive = LooseArchive.Merge(
                source is null ? null : files.ReadAllBytes(source),
                looseFiles.Select(file => new ArchiveFile(Path.GetFileName(file), files.ReadAllBytes(file))).ToList());
            files.WriteAllBytes(cache, archive);
            return cache;
        }
    }

    private bool IsUpToDate(string cache, IEnumerable<string> inputs) =>
        files.FileExists(cache) && inputs.All(input => files.LastWriteTimeUtc(input) <= files.LastWriteTimeUtc(cache));
}
