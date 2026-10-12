using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods;

public sealed class ModFileIndex
{
    private readonly Dictionary<string, string> _files;

    private ModFileIndex(Dictionary<string, string> files, IReadOnlyDictionary<string, int> filesByMod)
    {
        _files = files;
        FilesByMod = filesByMod;
    }

    public int Count => _files.Count;

    public IReadOnlyDictionary<string, int> FilesByMod { get; }

    public static ModFileIndex Build(IEnumerable<ModFolder> modsByPriority, IFileSystem fileSystem)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var filesByMod = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in modsByPriority)
            filesByMod[mod.Name] = mod.DataRoots.Sum(dataRoot => AddDataRoot(dataRoot, files, fileSystem));

        return new ModFileIndex(files, filesByMod);
    }

    public string? Resolve(string key) => _files.GetValueOrDefault(key);

    private static int AddDataRoot(ModDataRoot dataRoot, Dictionary<string, string> files, IFileSystem fileSystem)
    {
        var added = 0;

        foreach (var file in fileSystem.EnumerateFilesRecursively(dataRoot.Path))
        {
            var key = Path.Join(dataRoot.MountedAs, Path.GetRelativePath(dataRoot.Path, file));

            if (files.TryAdd(key, file))
                added++;
        }

        return added;
    }
}
