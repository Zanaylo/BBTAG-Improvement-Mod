using System.Collections.Concurrent;
using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods.Archives;

public sealed class ArchiveFileSystem(IFileSystem disk) : IFileSystem
{
    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    private readonly ConcurrentDictionary<string, OpenedArchive> _archives = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _opening = new();

    public string? ArchiveOf(string path) => ArchivePathIn(path);

    public long? LengthOf(string path) =>
        Locate(path) is { } inside && inside.Archive.HasFile(inside.Relative) ? inside.Archive.LengthOf(inside.Relative) : null;

    public bool DirectoryExists(string path) =>
        Locate(path) is { } inside ? inside.Archive.HasFolder(inside.Relative) : disk.DirectoryExists(path);

    public bool FileExists(string path)
    {
        if (ArchivePathIn(path) is not { } archivePath)
            return disk.FileExists(path);

        var relative = RelativeIn(path, archivePath);
        return relative.Length == 0 || ArchiveAt(archivePath).HasFile(relative);
    }

    public IEnumerable<string> EnumerateDirectories(string path) =>
        Locate(path) is { } inside ? InArchive(inside, inside.Archive.FoldersIn) : disk.EnumerateDirectories(path);

    public IEnumerable<string> EnumerateFiles(string path) =>
        Locate(path) is { } inside ? InArchive(inside, inside.Archive.FilesIn) : disk.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFilesRecursively(string path) =>
        Locate(path) is { } inside ? InArchive(inside, inside.Archive.FilesUnder) : disk.EnumerateFilesRecursively(path);

    public string ReadAllText(string path)
    {
        if (Locate(path) is not { } inside)
            return disk.ReadAllText(path);

        using var reader = new StreamReader(new MemoryStream(inside.Archive.Read(inside.Relative)), detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public byte[] ReadAllBytes(string path) => Locate(path) is { } inside ? inside.Archive.Read(inside.Relative) : disk.ReadAllBytes(path);

    public Stream OpenRead(string path) =>
        Locate(path) is { } inside ? new MemoryStream(inside.Archive.Read(inside.Relative), writable: false) : disk.OpenRead(path);

    public byte[] ReadBytes(string path, long offset, int length)
    {
        if (Locate(path) is not { } inside)
            return disk.ReadBytes(path, offset, length);

        var bytes = inside.Archive.Read(inside.Relative);

        if (offset < 0 || offset + length > bytes.Length)
            throw new EndOfStreamException($"{path} is shorter than {offset + length} bytes.");

        return bytes.AsSpan((int)offset, length).ToArray();
    }

    public DateTime LastWriteTimeUtc(string path) => disk.LastWriteTimeUtc(ArchivePathIn(path) ?? path);

    public Stream CreateFile(string path) => OnDiskOnly(path).CreateFile(path);

    public void WriteAllText(string path, string text) => OnDiskOnly(path).WriteAllText(path, text);

    public void WriteAllBytes(string path, byte[] bytes) => OnDiskOnly(path).WriteAllBytes(path, bytes);

    public void DeleteFile(string path) => OnDiskOnly(path).DeleteFile(path);

    public void DeleteDirectory(string path) => OnDiskOnly(path).DeleteDirectory(path);

    public void MoveFile(string source, string destination) => OnDiskOnly(source, destination).MoveFile(source, destination);

    public Version? FileVersion(string path) => OnDiskOnly(path).FileVersion(path);

    private IFileSystem OnDiskOnly(params string[] paths)
    {
        if (paths.Select(ArchivePathIn).FirstOrDefault(archive => archive is not null) is { } archive)
            throw new IOException($"{paths[0]} is inside {Path.GetFileName(archive)}, and files inside an archive cannot be changed.");

        return disk;
    }

    private static IEnumerable<string> InArchive(PathInArchive inside, Func<string, IEnumerable<string>> list)
    {
        if (!inside.Archive.HasFolder(inside.Relative))
            throw new DirectoryNotFoundException($"{inside.Relative} is not a folder in {Path.GetFileName(inside.ArchivePath)}.");

        return list(inside.Relative).Select(relative => Path.Join(inside.ArchivePath, relative)).ToArray();
    }

    private PathInArchive? Locate(string path)
    {
        return ArchivePathIn(path) is { } archivePath ? new PathInArchive(archivePath, ArchiveAt(archivePath), RelativeIn(path, archivePath)) : null;
    }

    private static string RelativeIn(string path, string archivePath) =>
        Path.TrimEndingDirectorySeparator(path[archivePath.Length..].TrimStart(Separators).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

    private string? ArchivePathIn(string path)
    {
        var start = 0;

        while (true)
        {
            var end = path.IndexOfAny(Separators, start);
            var candidate = end < 0 ? path : path[..end];

            if (ModArchiveName.IsArchive(candidate) && disk.FileExists(candidate))
                return candidate;

            if (end < 0)
                return null;

            start = end + 1;
        }
    }

    private ModArchive ArchiveAt(string archivePath)
    {
        var written = disk.LastWriteTimeUtc(archivePath);

        if (_archives.TryGetValue(archivePath, out var opened) && opened.LastWriteTimeUtc == written)
            return opened.Archive;

        lock (_opening)
        {
            if (_archives.TryGetValue(archivePath, out opened) && opened.LastWriteTimeUtc == written)
                return opened.Archive;

            var archive = ModArchive.Open(archivePath, disk);
            _archives[archivePath] = new OpenedArchive(written, archive);
            opened?.Archive.Dispose();
            return archive;
        }
    }

    private sealed record OpenedArchive(DateTime LastWriteTimeUtc, ModArchive Archive);

    private sealed record PathInArchive(string ArchivePath, ModArchive Archive, string Relative);
}
