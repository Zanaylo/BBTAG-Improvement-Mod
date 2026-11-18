using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods.Assets;

public sealed class EncryptedAssetView(IFileSystem disk, IEnumerable<string> assetFolders) : IFileSystem
{
    private const string DataFolder = "data";

    private readonly string[] _assetFolders = [.. assetFolders.Select(Path.TrimEndingDirectorySeparator)];

    public bool DirectoryExists(string path) => !IsInsideTheView(path) && disk.DirectoryExists(path);

    public bool FileExists(string path) => AssetBehind(path) is { } asset ? disk.FileExists(asset) : disk.FileExists(path);

    public IEnumerable<string> EnumerateDirectories(string path) => IsInsideTheView(path) ? [] : disk.EnumerateDirectories(path);

    public IEnumerable<string> EnumerateFiles(string path) => IsInsideTheView(path) ? [] : disk.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFilesRecursively(string path) => IsInsideTheView(path) ? [] : disk.EnumerateFilesRecursively(path);

    public string ReadAllText(string path)
    {
        if (AssetBehind(path) is null)
            return disk.ReadAllText(path);

        using var reader = new StreamReader(new MemoryStream(ReadAllBytes(path)), detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public byte[] ReadAllBytes(string path)
    {
        if (AssetBehind(path) is not { } asset)
            return disk.ReadAllBytes(path);

        var bytes = disk.ReadAllBytes(asset);
        EncryptedAssets.Decrypt(bytes, 0, KeyStartOf(asset));
        return bytes;
    }

    public Stream OpenRead(string path) =>
        AssetBehind(path) is null ? disk.OpenRead(path) : new MemoryStream(ReadAllBytes(path), writable: false);

    public byte[] ReadBytes(string path, long offset, int length)
    {
        if (AssetBehind(path) is not { } asset)
            return disk.ReadBytes(path, offset, length);

        var bytes = disk.ReadBytes(asset, offset, length);
        EncryptedAssets.Decrypt(bytes, offset, KeyStartOf(asset));
        return bytes;
    }

    public DateTime LastWriteTimeUtc(string path) => disk.LastWriteTimeUtc(AssetBehind(path) ?? path);

    public Stream CreateFile(string path) => disk.CreateFile(path);

    public void WriteAllText(string path, string text) => disk.WriteAllText(path, text);

    public void WriteAllBytes(string path, byte[] bytes) => disk.WriteAllBytes(path, bytes);

    public void DeleteFile(string path) => disk.DeleteFile(path);

    public void DeleteDirectory(string path) => disk.DeleteDirectory(path);

    public void MoveFile(string source, string destination) => disk.MoveFile(source, destination);

    public Version? FileVersion(string path) => disk.FileVersion(path);

    private string? AssetBehind(string path)
    {
        if (DataViewOf(path) is not { } view || EncryptedAssets.IsAssetName(view.Relative))
            return null;

        return Path.Join(view.Folder, EncryptedAssets.NameOf(Path.Join(DataFolder, view.Relative)));
    }

    private bool IsInsideTheView(string path) => DataViewOf(path) is not null;

    private (string Folder, string Relative)? DataViewOf(string path)
    {
        var trimmed = Path.TrimEndingDirectorySeparator(path);

        foreach (var folder in _assetFolders)
        {
            if (trimmed.Length > folder.Length + 1
                && trimmed.StartsWith(folder, StringComparison.OrdinalIgnoreCase)
                && trimmed[folder.Length] == Path.DirectorySeparatorChar)
                return (folder, trimmed[(folder.Length + 1)..]);
        }

        return null;
    }

    private static int KeyStartOf(string asset) => EncryptedAssets.KeyStartOf(Path.GetFileName(asset));
}
