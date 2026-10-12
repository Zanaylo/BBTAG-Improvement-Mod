namespace BBTAG_IM.Core.IO;

public interface IFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    IEnumerable<string> EnumerateDirectories(string path);

    IEnumerable<string> EnumerateFiles(string path);

    IEnumerable<string> EnumerateFilesRecursively(string path);

    string ReadAllText(string path);

    byte[] ReadAllBytes(string path);

    DateTime LastWriteTimeUtc(string path);

    void WriteAllText(string path, string text);

    void WriteAllBytes(string path, byte[] bytes);

    Version? FileVersion(string path);
}
