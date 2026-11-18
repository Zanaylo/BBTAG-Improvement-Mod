using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public sealed class ModDiscoveryTests : IDisposable
{
    private const string ModsDirectory = @"D:\BBTAG_IM\mods";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly string _realMods = Path.Combine(Path.GetTempPath(), "bbtag-discovery-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void InModsDirectory_FoldersWithAndWithoutGameFiles_ReturnsEveryFolderSortedByName()
    {
        // Arrange
        string zeta = Path.Combine(ModsDirectory, "Zeta");
        string alpha = Path.Combine(ModsDirectory, "alpha");
        string empty = Path.Combine(ModsDirectory, "Empty");
        _fileSystemMock.DirectoryExists(ModsDirectory).Returns(true);
        _fileSystemMock.EnumerateDirectories(ModsDirectory).Returns([zeta, empty, alpha]);
        _fileSystemMock.DirectoryExists(Arg.Is<string>(path => path == zeta || path == alpha || path == empty)).Returns(true);
        _fileSystemMock.DirectoryExists(Path.Combine(zeta, "data")).Returns(true);
        _fileSystemMock.DirectoryExists(Path.Combine(alpha, "data2")).Returns(true);

        // Act
        IReadOnlyList<ModFolder> mods = ModDiscovery.InModsDirectory(ModsDirectory, _fileSystemMock);

        // Assert
        Assert.Equal(["alpha", "Empty", "Zeta"], mods.Select(mod => mod.Name));
    }

    [Theory]
    [InlineData(".unpacking-Foo")]
    [InlineData(".archive-cache")]
    public void InModsDirectory_FolderStartingWithADot_IsNotAMod(string name)
    {
        // Arrange
        string hidden = Path.Combine(ModsDirectory, name);
        _fileSystemMock.DirectoryExists(ModsDirectory).Returns(true);
        _fileSystemMock.EnumerateDirectories(ModsDirectory).Returns([hidden]);
        _fileSystemMock.DirectoryExists(hidden).Returns(true);
        _fileSystemMock.DirectoryExists(Path.Combine(hidden, "data")).Returns(true);

        // Act
        IReadOnlyList<ModFolder> mods = ModDiscovery.InModsDirectory(ModsDirectory, _fileSystemMock);

        // Assert
        Assert.Empty(mods);
    }

    [Fact]
    public void InModsDirectory_ArchiveInTheModsFolder_IsAMod()
    {
        // Arrange
        string mods = RealModsFolder();
        TestArchives.Zip(Path.Combine(mods, "Bar.zip"), ("data/note.txt", "hello"));
        Directory.CreateDirectory(Path.Combine(mods, "Foo", "data"));

        // Act
        IReadOnlyList<ModFolder> found = ModDiscovery.InModsDirectory(mods, new ArchiveFileSystem(new PhysicalFileSystem()));

        // Assert
        Assert.Equal(["Bar.zip", "Foo"], found.Select(mod => mod.Name));
    }

    [Fact]
    public void InModsDirectory_FileThatIsNotAnArchive_IsNotAMod()
    {
        // Arrange
        string mods = RealModsFolder();
        File.WriteAllText(Path.Combine(mods, "readme.txt"), "x");

        // Act
        IReadOnlyList<ModFolder> found = ModDiscovery.InModsDirectory(mods, new ArchiveFileSystem(new PhysicalFileSystem()));

        // Assert
        Assert.Empty(found);
    }

    [Fact]
    public void InModsDirectory_DamagedArchive_IsNotAMod()
    {
        // Arrange
        string mods = RealModsFolder();
        File.WriteAllBytes(Path.Combine(mods, "Bar.zip"), [1, 2, 3]);

        // Act
        IReadOnlyList<ModFolder> found = ModDiscovery.InModsDirectory(mods, new ArchiveFileSystem(new PhysicalFileSystem()));

        // Assert
        Assert.Empty(found);
    }

    [Fact]
    public void InModsDirectory_FolderUnpackedFromAnArchiveThatIsStillThere_IsLeftOut()
    {
        // Arrange
        string mods = RealModsFolder();
        TestArchives.Zip(Path.Combine(mods, "Bar.zip"), ("data/note.txt", "hello"));
        Directory.CreateDirectory(Path.Combine(mods, "Bar", "data"));
        File.WriteAllText(Path.Combine(mods, "Bar", ".mod-archive"), "638000000000000000");

        // Act
        IReadOnlyList<ModFolder> found = ModDiscovery.InModsDirectory(mods, new ArchiveFileSystem(new PhysicalFileSystem()));

        // Assert
        Assert.Equal(["Bar.zip"], found.Select(mod => mod.Name));
    }

    [Fact]
    public void InModsDirectory_FolderNamedLikeAnArchiveButNotUnpackedFromIt_IsKept()
    {
        // Arrange
        string mods = RealModsFolder();
        TestArchives.Zip(Path.Combine(mods, "Bar.zip"), ("data/note.txt", "hello"));
        Directory.CreateDirectory(Path.Combine(mods, "Bar", "data"));

        // Act
        IReadOnlyList<ModFolder> found = ModDiscovery.InModsDirectory(mods, new ArchiveFileSystem(new PhysicalFileSystem()));

        // Assert
        Assert.Equal(["Bar", "Bar.zip"], found.Select(mod => mod.Name));
    }

    public void Dispose()
    {
        if (Directory.Exists(_realMods))
            Directory.Delete(_realMods, recursive: true);
    }

    private string RealModsFolder()
    {
        Directory.CreateDirectory(_realMods);
        return _realMods;
    }

    [Fact]
    public void InModsDirectory_MissingDirectory_ReturnsEmpty()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(ModsDirectory).Returns(false);

        // Act
        IReadOnlyList<ModFolder> mods = ModDiscovery.InModsDirectory(ModsDirectory, _fileSystemMock);

        // Assert
        Assert.Empty(mods);
    }
}
