using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods;

public sealed class DevOverlay(IFileSystem files, string devFolder, Func<string, string?> original)
{
    public const string CacheFolder = ".cache";

    private const string ArchiveExtension = ".pac";

    private static readonly string[] EditorExtensions = [".py", ".pyi", ".cs"];

    private readonly Lock _building = new();

    public string? Resolve(string key)
    {
        var loose = Path.Join(devFolder, key);

        if (files.FileExists(loose))
            return loose;

        var looseFiles = LooseArchiveFiles(key);
        return looseFiles.Count == 0 ? null : Built(key, looseFiles);
    }

    public bool Provides(string key) => files.FileExists(Path.Join(devFolder, key)) || LooseArchiveFiles(key).Count > 0;

    private List<string> LooseArchiveFiles(string key)
    {
        if (!key.EndsWith(ArchiveExtension, StringComparison.OrdinalIgnoreCase))
            return [];

        var folder = Path.Join(devFolder, key)[..^ArchiveExtension.Length];

        if (!files.DirectoryExists(folder))
            return [];

        return files.EnumerateFiles(folder)
            .Where(file => !EditorExtensions.Any(extension => file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            .ToList();
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
