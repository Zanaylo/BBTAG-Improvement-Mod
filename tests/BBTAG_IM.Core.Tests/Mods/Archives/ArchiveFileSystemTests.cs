using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods.Archives;

public sealed class ArchiveFileSystemTests : IDisposable
{
    private readonly string _mods = Path.Combine(Path.GetTempPath(), "bbtag-archive-fs-" + Guid.NewGuid().ToString("N"));
    private readonly string _zip;

    public ArchiveFileSystemTests()
    {
        Directory.CreateDirectory(_mods);
        _zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("data/char/note.txt", "hello"), ("data/top.txt", "top"), ("readme.txt", "world"));
    }

    public void Dispose() => Directory.Delete(_mods, recursive: true);

    public ArchiveFileSystem CreateArchiveFileSystem() => new(new PhysicalFileSystem());

    [Fact]
    public void DirectoryExists_Archive_ReturnsTrue()
    {
        // Act
        bool exists = CreateArchiveFileSystem().DirectoryExists(_zip);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void DirectoryExists_FolderInsideTheArchive_ReturnsTrue()
    {
        // Act
        bool exists = CreateArchiveFileSystem().DirectoryExists(Path.Combine(_zip, "data", "char"));

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void DirectoryExists_FolderMissingFromTheArchive_ReturnsFalse()
    {
        // Act
        bool exists = CreateArchiveFileSystem().DirectoryExists(Path.Combine(_zip, "asset"));

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void FileExists_EntryOfTheArchive_ReturnsTrue()
    {
        // Act
        bool exists = CreateArchiveFileSystem().FileExists(Path.Combine(_zip, "data", "char", "note.txt"));

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void FileExists_EntryMissingFromTheArchive_ReturnsFalse()
    {
        // Act
        bool exists = CreateArchiveFileSystem().FileExists(Path.Combine(_zip, "data", "missing.txt"));

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void FileExists_TheArchiveItself_ReturnsTrue()
    {
        // Act
        bool exists = CreateArchiveFileSystem().FileExists(_zip);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ReadAllText_EntryOfTheArchive_ReturnsItsText()
    {
        // Act
        string text = CreateArchiveFileSystem().ReadAllText(Path.Combine(_zip, "readme.txt"));

        // Assert
        Assert.Equal("world", text);
    }

    [Fact]
    public void ReadAllBytes_EntryOfTheArchive_ReturnsItsBytes()
    {
        // Act
        byte[] bytes = CreateArchiveFileSystem().ReadAllBytes(Path.Combine(_zip, "data", "char", "note.txt"));

        // Assert
        Assert.Equal("hello"u8.ToArray(), bytes);
    }

    [Fact]
    public void ReadBytes_PartOfAnEntry_ReturnsThoseBytes()
    {
        // Act
        byte[] bytes = CreateArchiveFileSystem().ReadBytes(Path.Combine(_zip, "data", "char", "note.txt"), 1, 3);

        // Assert
        Assert.Equal("ell"u8.ToArray(), bytes);
    }

    [Fact]
    public void ReadBytes_PastTheEndOfAnEntry_ThrowsEndOfStream()
    {
        // Act
        Exception? exception = Record.Exception(() => CreateArchiveFileSystem().ReadBytes(Path.Combine(_zip, "readme.txt"), 3, 10));

        // Assert
        Assert.IsType<EndOfStreamException>(exception);
    }

    [Fact]
    public void OpenRead_EntryOfTheArchive_StreamsItsBytes()
    {
        // Arrange
        using Stream stream = CreateArchiveFileSystem().OpenRead(Path.Combine(_zip, "readme.txt"));
        using StreamReader reader = new(stream);

        // Act
        string text = reader.ReadToEnd();

        // Assert
        Assert.Equal("world", text);
    }

    [Fact]
    public void EnumerateFiles_FolderInsideTheArchive_ListsItsFilesAsPathsInsideTheArchive()
    {
        // Act
        IEnumerable<string> files = CreateArchiveFileSystem().EnumerateFiles(Path.Combine(_zip, "data"));

        // Assert
        Assert.Equal([Path.Combine(_zip, "data", "top.txt")], files);
    }

    [Fact]
    public void EnumerateFilesRecursively_FolderInsideTheArchive_ListsEveryFileBelowIt()
    {
        // Act
        IEnumerable<string> files = CreateArchiveFileSystem().EnumerateFilesRecursively(Path.Combine(_zip, "data"));

        // Assert
        Assert.Equal([Path.Combine(_zip, "data", "char", "note.txt"), Path.Combine(_zip, "data", "top.txt")], files.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EnumerateDirectories_Archive_ListsItsTopFolders()
    {
        // Act
        IEnumerable<string> folders = CreateArchiveFileSystem().EnumerateDirectories(_zip);

        // Assert
        Assert.Equal([Path.Combine(_zip, "data")], folders);
    }

    [Fact]
    public void LengthOf_EntryOfTheArchive_ReturnsItsSize()
    {
        // Act
        long? length = CreateArchiveFileSystem().LengthOf(Path.Combine(_zip, "readme.txt"));

        // Assert
        Assert.Equal(5, length);
    }

    [Fact]
    public void ArchiveOf_EntryOfTheArchive_ReturnsTheArchiveFile()
    {
        // Act
        string? archive = CreateArchiveFileSystem().ArchiveOf(Path.Combine(_zip, "data", "top.txt"));

        // Assert
        Assert.Equal(_zip, archive);
    }

    [Fact]
    public void ArchiveOf_FileOnDisk_ReturnsNull()
    {
        // Arrange
        string onDisk = Path.Combine(_mods, "plain.txt");
        File.WriteAllText(onDisk, "x");

        // Act
        string? archive = CreateArchiveFileSystem().ArchiveOf(onDisk);

        // Assert
        Assert.Null(archive);
    }

    [Fact]
    public void ArchiveOf_FolderNamedLikeAnArchive_ReturnsNull()
    {
        // Arrange
        string folder = Path.Combine(_mods, "Folder.zip");
        Directory.CreateDirectory(Path.Combine(folder, "data"));

        // Act
        string? archive = CreateArchiveFileSystem().ArchiveOf(Path.Combine(folder, "data"));

        // Assert
        Assert.Null(archive);
    }

    [Fact]
    public void ReadAllText_ArchiveReplacedWithNewEntries_ReadsTheNewArchive()
    {
        // Arrange
        ArchiveFileSystem files = CreateArchiveFileSystem();
        files.ReadAllText(Path.Combine(_zip, "readme.txt"));
        TestArchives.Zip(_zip, ("readme.txt", "new"));
        File.SetLastWriteTimeUtc(_zip, DateTime.UtcNow.AddMinutes(5));

        // Act
        string text = files.ReadAllText(Path.Combine(_zip, "readme.txt"));

        // Assert
        Assert.Equal("new", text);
    }

    [Fact]
    public void WriteAllText_InsideTheArchive_ThrowsIO()
    {
        // Act
        Exception? exception = Record.Exception(() => CreateArchiveFileSystem().WriteAllText(Path.Combine(_zip, "readme.txt"), "x"));

        // Assert
        Assert.IsType<IOException>(exception);
    }

    [Fact]
    public void DeleteFile_InsideTheArchive_ThrowsIO()
    {
        // Act
        Exception? exception = Record.Exception(() => CreateArchiveFileSystem().DeleteFile(Path.Combine(_zip, "readme.txt")));

        // Assert
        Assert.IsType<IOException>(exception);
    }

    [Fact]
    public void ReadAllText_FileOutsideEveryArchive_ReadsItFromTheDisk()
    {
        // Arrange
        IFileSystem diskMock = Substitute.For<IFileSystem>();
        diskMock.ReadAllText(@"D:\mods\Foo\readme.txt").Returns("disk");
        ArchiveFileSystem files = new(diskMock);

        // Act
        string text = files.ReadAllText(@"D:\mods\Foo\readme.txt");

        // Assert
        Assert.Equal("disk", text);
    }

    [Fact]
    public void DirectoryExists_DamagedArchive_ThrowsModArchive()
    {
        // Arrange
        string damaged = Path.Combine(_mods, "Damaged.zip");
        File.WriteAllBytes(damaged, [1, 2, 3]);

        // Act
        Exception? exception = Record.Exception(() => CreateArchiveFileSystem().DirectoryExists(damaged));

        // Assert
        Assert.IsType<ModArchiveException>(exception);
    }
}
