namespace BBTAG_IM.Core.IO;

public interface IFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    IEnumerable<string> EnumerateDirectories(string path);

    IEnumerable<string> EnumerateFilesRecursively(string path);

    string ReadAllText(string path);
}
