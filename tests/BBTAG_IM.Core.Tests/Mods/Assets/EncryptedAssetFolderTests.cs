using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Assets;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods.Assets;

public class EncryptedAssetFolderTests
{
    private const string Game = @"D:\BBTAG";
    private const string AssetFolder = Game + @"\asset";
    private const string Key = @"data\char\char_ahe_pal.pac";
    private const string AssetFile = AssetFolder + @"\ac182c18ad5957273f33d5cc0ddd4fe8";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public EncryptedAssetFolder CreateEncryptedAssetFolder() => new(_fileSystemMock, Game);

    [Fact]
    public void Locate_KeyOnlyInTheAssetFolder_ReturnsTheAssetFile()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(AssetFolder).Returns(true);
        _fileSystemMock.FileExists(AssetFile).Returns(true);
        EncryptedAssetFolder assets = CreateEncryptedAssetFolder();

        // Act
        string? located = assets.Locate(Key);

        // Assert
        Assert.Equal(AssetFile, located);
    }

    [Fact]
    public void Locate_NoAssetFolder_ReturnsNull()
    {
        // Arrange
        _fileSystemMock.FileExists(AssetFile).Returns(true);
        EncryptedAssetFolder assets = CreateEncryptedAssetFolder();

        // Act
        string? located = assets.Locate(Key);

        // Assert
        Assert.Null(located);
    }

    [Fact]
    public void Locate_DecryptedFileInTheGameFolder_ReturnsNull()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(AssetFolder).Returns(true);
        _fileSystemMock.FileExists(AssetFile).Returns(true);
        _fileSystemMock.FileExists(Game + @"\" + Key).Returns(true);
        EncryptedAssetFolder assets = CreateEncryptedAssetFolder();

        // Act
        string? located = assets.Locate(Key);

        // Assert
        Assert.Null(located);
    }

    [Fact]
    public void Locate_AssetFileMissing_ReturnsNull()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(AssetFolder).Returns(true);
        EncryptedAssetFolder assets = CreateEncryptedAssetFolder();

        // Act
        string? located = assets.Locate(Key);

        // Assert
        Assert.Null(located);
    }

    [Fact]
    public void Locate_KeyOutsideTheDataFolder_ReturnsNull()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(AssetFolder).Returns(true);
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(callInfo => !callInfo.Arg<string>().StartsWith(Game + @"\Save", StringComparison.Ordinal));
        EncryptedAssetFolder assets = CreateEncryptedAssetFolder();

        // Act
        string? located = assets.Locate(@"Save\bbconfigs.ini");

        // Assert
        Assert.Null(located);
    }

    [Theory]
    [InlineData(AssetFile)]
    [InlineData(@"d:\bbtag\ASSET\ac182c18ad5957273f33d5cc0ddd4fe8")]
    public void KeyStartOf_FileInTheAssetFolder_ReturnsItsKeyStart(string path)
    {
        // Arrange
        EncryptedAssetFolder assets = CreateEncryptedAssetFolder();

        // Act
        int? keyStart = assets.KeyStartOf(path);

        // Assert
        Assert.Equal(26, keyStart);
    }

    [Theory]
    [InlineData(Game + @"\data\char\char_ahe_pal.pac")]
    [InlineData(Game + @"\mods\Overhaul\asset\ac182c18ad5957273f33d5cc0ddd4fe8")]
    [InlineData(AssetFolder + @"\readme.txt")]
    public void KeyStartOf_FileOutsideTheAssetFolderOrNotAnAsset_ReturnsNull(string path)
    {
        // Arrange
        EncryptedAssetFolder assets = CreateEncryptedAssetFolder();

        // Act
        int? keyStart = assets.KeyStartOf(path);

        // Assert
        Assert.Null(keyStart);
    }
}
