namespace BBTAG_IM.Core.IO;

public sealed class PhysicalFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    public IEnumerable<string> EnumerateFilesRecursively(string path) =>
        Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);

    public string ReadAllText(string path) => File.ReadAllText(path);
}
