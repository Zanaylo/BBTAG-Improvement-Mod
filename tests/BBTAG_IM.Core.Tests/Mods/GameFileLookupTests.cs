using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class GameFileLookupTests
{
    private const string Game = @"D:\BBTAG";
    private const string Dev = @"D:\BBTAG\mods\DEV";
    private const string Key = @"data\char\char_mko_scr.pac";
    private const string ModData = @"D:\BBTAG\mods\Overhaul\data";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Has_KeyAModProvides_ReturnsTrue()
    {
        // Arrange
        _fileSystemMock.EnumerateFilesRecursively(ModData).Returns([ModData + @"\char\char_mko_scr.pac"]);
        GameFileLookup lookup = CreateGameFileLookup(withDevFolder: false);

        // Act
        bool found = lookup.Has(Key);

        // Assert
        Assert.True(found);
    }

    [Fact]
    public void Has_KeyInTheGameFolder_ReturnsTrue()
    {
        // Arrange
        _fileSystemMock.FileExists(Game + @"\" + Key).Returns(true);
        GameFileLookup lookup = CreateGameFileLookup(withDevFolder: false);

        // Act
        bool found = lookup.Has(Key);

        // Assert
        Assert.True(found);
    }

    [Fact]
    public void Has_KeyInTheDevFolder_ReturnsTrue()
    {
        // Arrange
        _fileSystemMock.FileExists(Dev + @"\" + Key).Returns(true);
        GameFileLookup lookup = CreateGameFileLookup(withDevFolder: true);

        // Act
        bool found = lookup.Has(Key);

        // Assert
        Assert.True(found);
    }

    [Fact]
    public void Has_KeyInTheDevFolderTurnedOff_ReturnsFalse()
    {
        // Arrange
        _fileSystemMock.FileExists(Dev + @"\" + Key).Returns(true);
        GameFileLookup lookup = CreateGameFileLookup(withDevFolder: false);

        // Act
        bool found = lookup.Has(Key);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void Has_KeyNowhere_ReturnsFalse()
    {
        // Arrange
        GameFileLookup lookup = CreateGameFileLookup(withDevFolder: true);

        // Act
        bool found = lookup.Has(Key);

        // Assert
        Assert.False(found);
    }

    public GameFileLookup CreateGameFileLookup(bool withDevFolder)
    {
        ModFolder mod = new("Overhaul", @"D:\BBTAG\mods\Overhaul", [new ModDataRoot(ModData, "data")], null, null, []);
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);
        DevOverlay? dev = withDevFolder ? new DevOverlay(_fileSystemMock, Dev, _ => null) : null;
        return new GameFileLookup(_fileSystemMock, Game, index, dev);
    }
}
