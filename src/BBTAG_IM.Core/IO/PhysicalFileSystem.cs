using System.Diagnostics;

namespace BBTAG_IM.Core.IO;

public sealed class PhysicalFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFilesRecursively(string path) =>
        Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public Stream OpenRead(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

    public Stream CreateFile(string path)
    {
        CreateDirectoryOf(path);
        return File.Create(path);
    }

    public byte[] ReadBytes(string path, long offset, int length)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var bytes = new byte[length];
        file.Position = offset;
        file.ReadExactly(bytes);
        return bytes;
    }

    public DateTime LastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

    public void WriteAllText(string path, string text)
    {
        CreateDirectoryOf(path);
        File.WriteAllText(path, text);
    }

    public void WriteAllBytes(string path, byte[] bytes)
    {
        CreateDirectoryOf(path);
        File.WriteAllBytes(path, bytes);
    }

    public void DeleteFile(string path) => File.Delete(path);

    public void DeleteDirectory(string path) => Directory.Delete(path, recursive: true);

    public void MoveFile(string source, string destination)
    {
        CreateDirectoryOf(destination);
        File.Move(source, destination, overwrite: true);
    }

    public Version? FileVersion(string path)
    {
        var info = FileVersionInfo.GetVersionInfo(path);
        return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
    }

    private static void CreateDirectoryOf(string path)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }
}
