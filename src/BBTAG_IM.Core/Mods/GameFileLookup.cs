using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods;

public sealed class GameFileLookup(IFileSystem files, string gameDirectory, ModFileIndex index, DevOverlay? dev) : IGameFiles
{
    public bool Has(string key) =>
        dev?.Provides(key) == true
        || index.Resolve(key) is not null
        || files.FileExists(Path.Join(gameDirectory, key));
}
