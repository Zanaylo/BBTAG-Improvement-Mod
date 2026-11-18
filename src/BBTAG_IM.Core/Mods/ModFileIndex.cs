using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Assets;

namespace BBTAG_IM.Core.Mods;

public sealed class ModFileIndex
{
    private readonly Dictionary<string, RankedFile> _files;
    private readonly Dictionary<string, RankedFile> _assets;
    private readonly HashSet<string> _assetPaths;
    private readonly Lazy<Dictionary<string, RankedFile>> _plainByAssetName;

    private ModFileIndex(Dictionary<string, RankedFile> files, Dictionary<string, RankedFile> assets, IReadOnlyDictionary<string, int> filesByMod)
    {
        _files = files;
        _assets = assets;
        _assetPaths = assets.Values.Select(asset => asset.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _plainByAssetName = new Lazy<Dictionary<string, RankedFile>>(() => ByAssetName(files));
        FilesByMod = filesByMod;
    }

    public int Count => _files.Count + _assets.Count;

    public IReadOnlyDictionary<string, int> FilesByMod { get; }

    public static ModFileIndex Build(IEnumerable<ModFolder> modsByPriority, IFileSystem fileSystem)
    {
        var files = new Dictionary<string, RankedFile>(StringComparer.OrdinalIgnoreCase);
        var assets = new Dictionary<string, RankedFile>(StringComparer.OrdinalIgnoreCase);
        var filesByMod = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rank = 0;

        foreach (var mod in modsByPriority)
        {
            filesByMod[mod.Name] = mod.DataRoots.Sum(dataRoot => AddDataRoot(dataRoot, rank, files, fileSystem))
                + AddEncryptedAssets(mod.EncryptedAssetsPath, rank, assets, fileSystem);
            rank++;
        }

        return new ModFileIndex(files, assets, filesByMod);
    }

    public string? Resolve(string key)
    {
        if (EncryptedAssets.AssetNameIn(key) is { } assetName)
            return Preferred(_plainByAssetName.Value.GetValueOrDefault(assetName), _assets.GetValueOrDefault(assetName));

        return Preferred(_files.GetValueOrDefault(key), EncryptedAssetOf(key));
    }

    public string? ResolvePlain(string key) => _files.GetValueOrDefault(key)?.Path;

    public int? AssetKeyStartOf(string path) =>
        _assetPaths.Contains(path) ? EncryptedAssets.KeyStartOf(Path.GetFileName(path)) : null;

    private static string? Preferred(RankedFile? plain, RankedFile? encrypted) =>
        encrypted is not null && (plain is null || encrypted.Rank < plain.Rank) ? encrypted.Path : plain?.Path;

    private static Dictionary<string, RankedFile> ByAssetName(Dictionary<string, RankedFile> files)
    {
        var byName = new Dictionary<string, RankedFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, file) in files.Where(entry => EncryptedAssets.IsInDataFolder(entry.Key)))
            byName.TryAdd(EncryptedAssets.NameOf(key), file);

        return byName;
    }

    private RankedFile? EncryptedAssetOf(string key) =>
        _assets.Count > 0 && EncryptedAssets.IsInDataFolder(key) ? _assets.GetValueOrDefault(EncryptedAssets.NameOf(key)) : null;

    private static int AddDataRoot(ModDataRoot dataRoot, int rank, Dictionary<string, RankedFile> files, IFileSystem fileSystem)
    {
        var added = 0;

        foreach (var file in fileSystem.EnumerateFilesRecursively(dataRoot.Path))
        {
            var key = Path.Join(dataRoot.MountedAs, Path.GetRelativePath(dataRoot.Path, file));

            if (files.TryAdd(key, new RankedFile(file, rank)))
                added++;
        }

        return added;
    }

    private static int AddEncryptedAssets(string? folder, int rank, Dictionary<string, RankedFile> assets, IFileSystem fileSystem)
    {
        if (folder is null)
            return 0;

        return fileSystem.EnumerateFiles(folder)
            .Where(file => EncryptedAssets.IsAssetName(Path.GetFileName(file)))
            .Count(file => assets.TryAdd(Path.GetFileName(file), new RankedFile(file, rank)));
    }

    private sealed record RankedFile(string Path, int Rank);
}
