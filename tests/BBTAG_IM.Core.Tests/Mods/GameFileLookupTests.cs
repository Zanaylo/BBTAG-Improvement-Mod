using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Mods.Assets;
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
    public void Has_KeyOnlyInTheEncryptedAssetFolder_ReturnsTrue()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(Game + @"\asset").Returns(true);
        _fileSystemMock.FileExists(Game + @"\asset\17b67f07e56eb72ada68a4e7f962fd18").Returns(true);
        GameFileLookup lookup = CreateGameFileLookup(withDevFolder: false);

        // Act
        bool found = lookup.Has(Key);

        // Assert
        Assert.True(found);
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
        return new GameFileLookup(_fileSystemMock, Game, index, dev, new EncryptedAssetFolder(_fileSystemMock, Game));
    }
}
