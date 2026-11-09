using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Sprites;

public class GameArchiveFileTests
{
    private const string ModData = @"D:\BBTAG\mods\Fonts\data";
    private const string GameData = @"D:\BBTAG\data";
    private const string Archive = @"gr\gr_johchu_cmn.pac";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public GameArchiveFile CreateGameArchiveFile() => new(_fileSystemMock, [ModData, GameData]);

    [Fact]
    public void Read_FileInsideANestedArchive_ReturnsItsBytes()
    {
        // Arrange
        byte[] inner = SpriteFiles.Fpac(("other.hip", [1, 2]), ("bb4font0.hip", [7, 8, 9]));
        Exists(GameData, SpriteFiles.Fpac(("fhd.pac", inner)));
        GameArchiveFile archive = CreateGameArchiveFile();

        // Act
        byte[]? bytes = archive.Read(Archive, "bb4font0.hip");

        // Assert
        Assert.Equal([7, 8, 9], bytes);
    }

    [Fact]
    public void Read_ModShipsTheArchive_PrefersTheModCopy()
    {
        // Arrange
        Exists(ModData, SpriteFiles.Fpac(("bb4font0.hip", [1])));
        Exists(GameData, SpriteFiles.Fpac(("bb4font0.hip", [2])));
        GameArchiveFile archive = CreateGameArchiveFile();

        // Act
        byte[]? bytes = archive.Read(Archive, "bb4font0.hip");

        // Assert
        Assert.Equal([1], bytes);
    }

    [Fact]
    public void Read_FileNotInTheArchive_ReturnsNull()
    {
        // Arrange
        Exists(GameData, SpriteFiles.Fpac(("other.hip", [1])));
        GameArchiveFile archive = CreateGameArchiveFile();

        // Act
        byte[]? bytes = archive.Read(Archive, "bb4font0.hip");

        // Assert
        Assert.Null(bytes);
    }

    [Fact]
    public void Read_ArchiveMissing_ReturnsNull()
    {
        // Arrange
        GameArchiveFile archive = CreateGameArchiveFile();

        // Act
        byte[]? bytes = archive.Read(Archive, "bb4font0.hip");

        // Assert
        Assert.Null(bytes);
    }

    [Fact]
    public void Read_EncryptedArchive_ReturnsNull()
    {
        // Arrange
        Exists(GameData, [1, 2, 3, 4, 5, 6, 7, 8]);
        GameArchiveFile archive = CreateGameArchiveFile();

        // Act
        byte[]? bytes = archive.Read(Archive, "bb4font0.hip");

        // Assert
        Assert.Null(bytes);
    }

    private void Exists(string dataFolder, byte[] bytes)
    {
        var path = Path.Combine(dataFolder, Archive);
        _fileSystemMock.FileExists(path).Returns(true);
        _fileSystemMock.ReadAllBytes(path).Returns(bytes);
    }
}
