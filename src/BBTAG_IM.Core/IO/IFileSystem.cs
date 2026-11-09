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

    Stream OpenRead(string path);

    Stream CreateFile(string path);

    byte[] ReadBytes(string path, long offset, int length);

    DateTime LastWriteTimeUtc(string path);

    void WriteAllText(string path, string text);

    void WriteAllBytes(string path, byte[] bytes);

    void DeleteFile(string path);

    void DeleteDirectory(string path);

    void MoveFile(string source, string destination);

    Version? FileVersion(string path);
}
