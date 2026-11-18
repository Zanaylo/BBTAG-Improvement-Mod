using System.Text;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Mods.Archives;

public sealed class ModArchiveTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bbtag-mod-archive-" + Guid.NewGuid().ToString("N"));
    private readonly IFileSystem _files = new PhysicalFileSystem();

    public ModArchiveTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    public ModArchive CreateModArchive(string path) => ModArchive.Open(path, _files);

    [Theory]
    [InlineData(nameof(ArchiveFixtures.Rar), "Foo.rar")]
    [InlineData(nameof(ArchiveFixtures.SolidRar), "Foo.rar")]
    [InlineData(nameof(ArchiveFixtures.SevenZip), "Foo.7z")]
    [InlineData(nameof(ArchiveFixtures.Tar), "Foo.tar")]
    [InlineData(nameof(ArchiveFixtures.TarGz), "Foo.tar.gz")]
    public void Read_EntryOfARealArchive_ReturnsItsBytes(string fixture, string fileName)
    {
        // Arrange
        using ModArchive archive = CreateModArchive(TestArchives.Fixture(fixture, Path.Combine(_folder, fileName)));

        // Act
        byte[] note = archive.Read(@"Foo\data\note.txt");
        byte[] readme = archive.Read(@"Foo\readme.txt");

        // Assert
        Assert.Equal(("hello", "world"), (Encoding.ASCII.GetString(note), Encoding.ASCII.GetString(readme)));
    }

    [Fact]
    public void Read_EntryOfAZip_ReturnsItsBytes()
    {
        // Arrange
        using ModArchive archive = CreateModArchive(TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/note.txt", "hello")));

        // Act
        byte[] note = archive.Read(@"data\note.txt");

        // Assert
        Assert.Equal("hello", Encoding.ASCII.GetString(note));
    }

    [Fact]
    public void Read_PathInAnotherCase_ReturnsTheEntry()
    {
        // Arrange
        using ModArchive archive = CreateModArchive(TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("Data/Note.txt", "hello")));

        // Act
        byte[] note = archive.Read(@"DATA\note.TXT");

        // Assert
        Assert.Equal("hello", Encoding.ASCII.GetString(note));
    }

    [Fact]
    public void Read_MissingEntry_ThrowsFileNotFound()
    {
        // Arrange
        using ModArchive archive = CreateModArchive(TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/note.txt", "hello")));

        // Act
        Exception? exception = Record.Exception(() => archive.Read(@"data\other.txt"));

        // Assert
        Assert.IsType<FileNotFoundException>(exception);
    }

    [Fact]
    public void Files_Zip_ListsEveryFileWithBackslashes()
    {
        // Arrange
        string path = TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/char/a.pac", "a"), ("readme.txt", "b"), ("empty/", ""));

        // Act
        using ModArchive archive = CreateModArchive(path);

        // Assert
        Assert.Equal([@"data\char\a.pac", "readme.txt"], archive.Files.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Files_EntryEscapingTheArchive_IsLeftOut()
    {
        // Arrange
        string path = TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/note.txt", "x"), ("../../evil.txt", "y"));

        // Act
        using ModArchive archive = CreateModArchive(path);

        // Assert
        Assert.Equal([@"data\note.txt"], archive.Files);
    }

    [Fact]
    public void HasFolder_FolderOfAnEntry_ReturnsTrue()
    {
        // Arrange
        using ModArchive archive = CreateModArchive(TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/char/a.pac", "a")));

        // Act
        bool found = archive.HasFolder(@"DATA\char");

        // Assert
        Assert.True(found);
    }

    [Fact]
    public void HasFolder_Root_ReturnsTrue()
    {
        // Arrange
        using ModArchive archive = CreateModArchive(TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("readme.txt", "a")));

        // Act
        bool found = archive.HasFolder("");

        // Assert
        Assert.True(found);
    }

    [Fact]
    public void HasFolder_NameOfAFile_ReturnsFalse()
    {
        // Arrange
        using ModArchive archive = CreateModArchive(TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("readme.txt", "a")));

        // Act
        bool found = archive.HasFolder("readme.txt");

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void FilesIn_Folder_ListsOnlyItsDirectFiles()
    {
        // Arrange
        string path = TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/a.pac", "a"), ("data/char/b.pac", "b"), ("readme.txt", "c"));
        using ModArchive archive = CreateModArchive(path);

        // Act
        IEnumerable<string> files = archive.FilesIn("data");

        // Assert
        Assert.Equal([@"data\a.pac"], files);
    }

    [Fact]
    public void FilesUnder_Folder_ListsEveryFileBelowIt()
    {
        // Arrange
        string path = TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/a.pac", "a"), ("data/char/b.pac", "b"), ("readme.txt", "c"));
        using ModArchive archive = CreateModArchive(path);

        // Act
        IEnumerable<string> files = archive.FilesUnder("data");

        // Assert
        Assert.Equal([@"data\a.pac", @"data\char\b.pac"], files.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void FoldersIn_Root_ListsTheTopFolders()
    {
        // Arrange
        string path = TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/char/a.pac", "a"), ("asset/0123", "b"), ("readme.txt", "c"));
        using ModArchive archive = CreateModArchive(path);

        // Act
        IEnumerable<string> folders = archive.FoldersIn("");

        // Assert
        Assert.Equal(["asset", "data"], folders.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void LengthOf_Entry_ReturnsItsUncompressedSize()
    {
        // Arrange
        using ModArchive archive = CreateModArchive(TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/note.txt", "hello")));

        // Act
        long length = archive.LengthOf(@"data\note.txt");

        // Assert
        Assert.Equal(5, length);
    }

    [Fact]
    public void Open_PasswordProtectedArchive_ThrowsModArchive()
    {
        // Arrange
        string path = TestArchives.Fixture(nameof(ArchiveFixtures.PasswordRar), Path.Combine(_folder, "Foo.rar"));

        // Act
        Exception? exception = Record.Exception(() => CreateModArchive(path).Dispose());

        // Assert
        Assert.IsType<ModArchiveException>(exception);
    }

    [Fact]
    public void Open_DamagedArchive_ThrowsModArchive()
    {
        // Arrange
        string path = Path.Combine(_folder, "Bar.zip");
        File.WriteAllBytes(path, [1, 2, 3, 4, 5]);

        // Act
        Exception? exception = Record.Exception(() => CreateModArchive(path).Dispose());

        // Assert
        Assert.IsType<ModArchiveException>(exception);
    }

    [Fact]
    public void Open_Archive_LetsTheFileBeReplacedAfterwards()
    {
        // Arrange
        string path = TestArchives.Zip(Path.Combine(_folder, "Bar.zip"), ("data/note.txt", "hello"));
        using ModArchive archive = CreateModArchive(path);

        // Act
        Exception? exception = Record.Exception(() => File.Delete(path));

        // Assert
        Assert.Null(exception);
    }
}
