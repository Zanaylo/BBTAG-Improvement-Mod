using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Hantei.Games;

public sealed class UnpackedGameFiles(IFileSystem files, string root) : IHanteiGameFiles
{
    public IEnumerable<string> Names => files
        .EnumerateFilesRecursively(Path.Combine(root, HanteiFileName.DataFolder))
        .Select(path => HanteiFileName.Normalize(Path.GetRelativePath(root, path)));

    public bool Contains(string name) => files.FileExists(PathOf(name));

    public byte[] Read(string name) => files.ReadAllBytes(PathOf(name));

    private string PathOf(string name) => Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
}
