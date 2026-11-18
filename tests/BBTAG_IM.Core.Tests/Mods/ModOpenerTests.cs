using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Mods;

public sealed class ModOpenerTests : IDisposable
{
    private readonly string _mods = Path.Combine(Path.GetTempPath(), "bbtag-mod-opener-" + Guid.NewGuid().ToString("N"));
    private readonly ArchiveFileSystem _files = new(new PhysicalFileSystem());

    public ModOpenerTests() => Directory.CreateDirectory(_mods);

    public void Dispose() => Directory.Delete(_mods, recursive: true);

    [Fact]
    public void Open_Folder_InspectsTheFolder()
    {
        // Arrange
        string folder = Path.Combine(_mods, "Foo");
        Directory.CreateDirectory(Path.Combine(folder, "data"));

        // Act
        ModFolder mod = ModOpener.Open(folder, _files);

        // Assert
        Assert.Equal(("Foo", folder, null), (mod.Name, mod.RootPath, mod.ArchivePath));
    }

    [Fact]
    public void Open_Archive_IsAModNamedAfterTheArchiveFile()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("data/note.txt", "hello"));

        // Act
        ModFolder mod = ModOpener.Open(zip, _files);

        // Assert
        Assert.Equal(("Bar.zip", zip), (mod.Name, mod.ArchivePath));
    }

    [Fact]
    public void Open_Archive_MountsItsDataFolderInsideTheArchive()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("data/note.txt", "hello"));

        // Act
        ModFolder mod = ModOpener.Open(zip, _files);

        // Assert
        Assert.Equal([new ModDataRoot(Path.Combine(zip, "data"), "data")], mod.DataRoots);
    }

    [Fact]
    public void Open_ArchiveWithTheModInsideAFolder_UsesThatFolderAsTheRoot()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Unlimited.zip"), ("BBTAG-UNLIMITED/asset/ac182c18ad5957273f33d5cc0ddd4fe8", "x"));

        // Act
        ModFolder mod = ModOpener.Open(zip, _files);

        // Assert
        Assert.Equal(Path.Combine(zip, "BBTAG-UNLIMITED", "asset"), mod.EncryptedAssetsPath);
    }

    [Fact]
    public void Open_ArchiveWithoutGameFiles_IsAModWithNothingToLoad()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("photos/a.txt", "x"), ("music/b.txt", "y"));

        // Act
        ModFolder mod = ModOpener.Open(zip, _files);

        // Assert
        Assert.Equal(("Bar.zip", false), (mod.Name, mod.HasGameFiles));
    }

    [Fact]
    public void Open_ArchiveWithATable_CopiesTheTableOutOfTheArchive()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("BBTAG.CT", "<table/>"), ("data/note.txt", "hello"));

        // Act
        ModFolder mod = ModOpener.Open(zip, _files);

        // Assert
        Assert.Equal(Path.Combine(_mods, ".archive-cache", "Bar.zip", "BBTAG.CT"), mod.TablePath);
        Assert.Equal("<table/>", File.ReadAllText(mod.TablePath!));
    }

    [Fact]
    public void Open_ArchiveWithExecutableAndDlls_CopiesThemOutOfTheArchive()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("Bar/BBTAG.exe", "exe"), ("Bar/Plugin.dll", "dll"), ("Bar/data/note.txt", "hello"));
        string cache = Path.Combine(_mods, ".archive-cache", "Bar.zip");

        // Act
        ModFolder mod = ModOpener.Open(zip, _files);

        // Assert
        Assert.Equal((Path.Combine(cache, "BBTAG.exe"), Path.Combine(cache, "Plugin.dll")), (mod.ExecutablePath, Assert.Single(mod.PluginPaths)));
        Assert.Equal(("exe", "dll"), (File.ReadAllText(mod.ExecutablePath!), File.ReadAllText(mod.PluginPaths[0])));
    }

    [Fact]
    public void Open_ArchiveWithOnlyGameData_WritesNothingOutOfTheArchive()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("data/note.txt", "hello"));

        // Act
        ModOpener.Open(zip, _files);

        // Assert
        Assert.False(Directory.Exists(Path.Combine(_mods, ".archive-cache")));
    }

    [Fact]
    public void Open_ArchiveChangedAfterItsTableWasCopied_CopiesTheNewTable()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("BBTAG.CT", "old"));
        ModOpener.Open(zip, _files);
        TestArchives.Zip(zip, ("BBTAG.CT", "new"));
        File.SetLastWriteTimeUtc(zip, DateTime.UtcNow.AddMinutes(5));

        // Act
        ModFolder mod = ModOpener.Open(zip, _files);

        // Assert
        Assert.Equal("new", File.ReadAllText(mod.TablePath!));
    }

    [Fact]
    public void Open_ArchiveNotChanged_KeepsTheCopiedFiles()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("BBTAG.CT", "table"));
        ModFolder first = ModOpener.Open(zip, _files);
        File.WriteAllText(first.TablePath!, "edited copy");

        // Act
        ModFolder mod = ModOpener.Open(zip, _files);

        // Assert
        Assert.Equal("edited copy", File.ReadAllText(mod.TablePath!));
    }

    [Fact]
    public void Open_DamagedArchive_ThrowsInvalidModFolder()
    {
        // Arrange
        string zip = Path.Combine(_mods, "Bar.zip");
        File.WriteAllBytes(zip, [1, 2, 3, 4]);

        // Act
        Exception? exception = Record.Exception(() => ModOpener.Open(zip, _files));

        // Assert
        Assert.IsType<InvalidModFolderException>(exception);
    }

    [Fact]
    public void Open_ArchiveWithoutAnArchiveFileSystem_ThrowsInvalidModFolder()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("data/note.txt", "hello"));

        // Act
        Exception? exception = Record.Exception(() => ModOpener.Open(zip, new PhysicalFileSystem()));

        // Assert
        Assert.IsType<InvalidModFolderException>(exception);
    }

    [Fact]
    public void Open_MissingPath_ThrowsInvalidModFolder()
    {
        // Act
        Exception? exception = Record.Exception(() => ModOpener.Open(Path.Combine(_mods, "Missing"), _files));

        // Assert
        Assert.IsType<InvalidModFolderException>(exception);
    }

    [Fact]
    public void Open_ArchiveData_IsReadThroughTheIndex()
    {
        // Arrange
        string zip = TestArchives.Zip(Path.Combine(_mods, "Bar.zip"), ("data/char/note.txt", "hello"));
        ModFolder mod = ModOpener.Open(zip, _files);

        // Act
        string? resolved = ModFileIndex.Build([mod], _files).Resolve(@"data\char\note.txt");

        // Assert
        Assert.Equal(Path.Combine(zip, "data", "char", "note.txt"), resolved);
    }
}
