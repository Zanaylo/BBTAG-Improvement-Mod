using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods.Assets;

public sealed class EncryptedAssetFolder
{
    private readonly IFileSystem _files;
    private readonly string _gameDirectory;
    private readonly string _folder;
    private readonly bool _exists;

    public EncryptedAssetFolder(IFileSystem files, string gameDirectory)
    {
        _files = files;
        _gameDirectory = gameDirectory;
        _folder = Path.Join(gameDirectory, EncryptedAssets.FolderName);
        _exists = files.DirectoryExists(_folder);
    }

    public string? Locate(string key)
    {
        if (!_exists || !EncryptedAssets.IsInDataFolder(key))
            return null;

        if (_files.FileExists(Path.Join(_gameDirectory, key)))
            return null;

        var asset = Path.Join(_folder, EncryptedAssets.NameOf(key));
        return _files.FileExists(asset) ? asset : null;
    }

    public int? KeyStartOf(string path)
    {
        var name = Path.GetFileName(path);

        if (!EncryptedAssets.IsAssetName(name) || !_folder.Equals(Path.GetDirectoryName(path), StringComparison.OrdinalIgnoreCase))
            return null;

        return EncryptedAssets.KeyStartOf(name);
    }
}
