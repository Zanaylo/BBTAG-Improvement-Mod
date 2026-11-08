using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Launch;

public sealed class BundledFiles(IFileSystem files, string directory)
{
    public const string CacheFolder = "cache";

    public string Place(string fileName, byte[] bytes)
    {
        var cached = CachedPath(fileName);

        if (!IsCached(cached, bytes))
            files.WriteAllBytes(cached, bytes);

        return cached;
    }

    public string Place(string fileName, Stream source)
    {
        using var bytes = new MemoryStream();
        source.CopyTo(bytes);
        return Place(fileName, bytes.ToArray());
    }

    public string PathOf(string fileName)
    {
        var cached = CachedPath(fileName);
        return files.FileExists(cached) ? cached : Path.Combine(directory, fileName);
    }

    private string CachedPath(string fileName) => Path.Combine(directory, CacheFolder, fileName);

    private bool IsCached(string cached, byte[] bytes) =>
        files.FileExists(cached) && files.ReadAllBytes(cached).AsSpan().SequenceEqual(bytes);
}
