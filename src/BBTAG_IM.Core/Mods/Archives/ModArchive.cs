using BBTAG_IM.Core.IO;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace BBTAG_IM.Core.Mods.Archives;

public sealed class ModArchive : IDisposable
{
    private const string Root = "";

    private readonly string _path;
    private readonly IFileSystem _files;
    private readonly ArchiveReading _reading;
    private readonly Dictionary<string, ArchivedFile> _entries;
    private readonly HashSet<string> _folders;
    private readonly Lock _gate = new();

    private Stream? _openStream;
    private Dictionary<string, IArchiveEntry>? _openEntries;
    private IArchive? _openArchive;

    private ModArchive(string path, IFileSystem files, ArchiveReading reading, Dictionary<string, ArchivedFile> entries)
    {
        _path = path;
        _files = files;
        _reading = reading;
        _entries = entries;
        _folders = FoldersOf(entries.Keys);
    }

    public IReadOnlyCollection<string> Files => _entries.Keys;

    public static ModArchive Open(string path, IFileSystem files)
    {
        try
        {
            using var stream = files.OpenRead(path);
            using var archive = ArchiveFactory.Open(stream);
            var reading = ArchiveReadings.Of(archive);
            var entries = reading == ArchiveReading.CompressedTar ? ListCompressedTar(path, files) : List(path, archive.Entries);
            return new ModArchive(path, files, reading, entries);
        }
        catch (Exception exception) when (exception is not (ModArchiveException or OutOfMemoryException))
        {
            throw new ModArchiveException(exception.Message, exception);
        }
    }

    public bool HasFile(string relative) => _entries.ContainsKey(relative);

    public bool HasFolder(string relative) => relative.Length == 0 || _folders.Contains(relative);

    public long LengthOf(string relative) => FileAt(relative).Length;

    public IEnumerable<string> FilesIn(string folder) => _entries.Keys.Where(file => IsDirectlyIn(file, folder));

    public IEnumerable<string> FilesUnder(string folder) => _entries.Keys.Where(file => IsUnder(file, folder));

    public IEnumerable<string> FoldersIn(string folder) => _folders.Where(child => IsDirectlyIn(child, folder));

    public byte[] Read(string relative)
    {
        var file = FileAt(relative);

        try
        {
            lock (_gate)
                return _reading == ArchiveReading.EachEntry ? ReadEntry(file) : ReadInOrder(file);
        }
        catch (Exception exception) when (exception is not (ModArchiveException or OutOfMemoryException or ObjectDisposedException))
        {
            throw new ModArchiveException($"{file.Key} could not be read from {Path.GetFileName(_path)}: {exception.Message}", exception);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _openArchive?.Dispose();
            _openStream?.Dispose();
            _openArchive = null;
            _openStream = null;
            _openEntries = null;
        }
    }

    private ArchivedFile FileAt(string relative) =>
        _entries.GetValueOrDefault(relative) ?? throw new FileNotFoundException($"{relative} is not in {Path.GetFileName(_path)}.", Path.Join(_path, relative));

    private byte[] ReadEntry(ArchivedFile file)
    {
        var entry = OpenEntries()[file.Key];
        using var source = entry.OpenEntryStream();
        return Copied(source, file.Length);
    }

    private Dictionary<string, IArchiveEntry> OpenEntries()
    {
        if (_openEntries is not null)
            return _openEntries;

        _openStream = _files.OpenRead(_path);
        _openArchive = ArchiveFactory.Open(_openStream);
        _openEntries = _openArchive.Entries.Where(ArchiveReadings.IsFile).ToDictionary(entry => entry.Key!, StringComparer.Ordinal);
        return _openEntries;
    }

    private byte[] ReadInOrder(ArchivedFile file)
    {
        using var stream = _files.OpenRead(_path);
        using var archive = _reading == ArchiveReading.CompressedTar ? null : ArchiveFactory.Open(stream);
        using var reader = archive?.ExtractAllEntries() ?? ReaderFactory.Open(stream);

        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.Key != file.Key)
                continue;

            using var source = reader.OpenEntryStream();
            return Copied(source, file.Length);
        }

        throw new FileNotFoundException($"{file.Key} is not in {Path.GetFileName(_path)}.");
    }

    private static byte[] Copied(Stream source, long length)
    {
        using var copy = new MemoryStream(length > 0 && length <= int.MaxValue ? (int)length : 0);
        source.CopyTo(copy);
        return copy.ToArray();
    }

    private static Dictionary<string, ArchivedFile> ListCompressedTar(string path, IFileSystem files)
    {
        using var stream = files.OpenRead(path);
        using var reader = ReaderFactory.Open(stream);
        List<IEntry> entries = [];

        while (reader.MoveToNextEntry())
            entries.Add(reader.Entry);

        return List(path, entries);
    }

    private static Dictionary<string, ArchivedFile> List(string path, IEnumerable<IEntry> entries)
    {
        var files = new Dictionary<string, ArchivedFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries.Where(ArchiveReadings.IsFile))
        {
            if (entry.IsEncrypted)
                throw new ModArchiveException("it needs a password.");

            if (RelativePathOf(path, entry.Key!) is { } relative)
                files.TryAdd(relative, new ArchivedFile(entry.Key!, entry.Size));
        }

        return files;
    }

    private static string? RelativePathOf(string path, string key)
    {
        try
        {
            return Path.GetRelativePath(path, ArchiveEntryPath.Inside(path, key));
        }
        catch (ModArchiveException)
        {
            return null;
        }
    }

    private static HashSet<string> FoldersOf(IEnumerable<string> files)
    {
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var folder = ParentOf(file);

            while (folder.Length > 0 && folders.Add(folder))
                folder = ParentOf(folder);
        }

        return folders;
    }

    private static bool IsDirectlyIn(string path, string folder) => ParentOf(path).Equals(folder, StringComparison.OrdinalIgnoreCase);

    private static bool IsUnder(string path, string folder) =>
        folder.Length == 0 || path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static string ParentOf(string path) => Path.GetDirectoryName(path) ?? Root;

    private sealed record ArchivedFile(string Key, long Length);
}
