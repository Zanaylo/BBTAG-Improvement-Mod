using System.Text;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Assets;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods.Assets;

public class EncryptedAssetViewTests
{
    private const string AssetFolder = @"D:\BBTAG\asset";
    private const string DataView = AssetFolder + @"\char\char_ahe_pal.pac";
    private const string AssetName = "ac182c18ad5957273f33d5cc0ddd4fe8";
    private const string AssetFile = AssetFolder + @"\" + AssetName;
    private const int AssetKeyStart = 26;

    private static readonly byte[] Plain = Encoding.ASCII.GetBytes("FPAC palette bytes");

    private readonly IFileSystem _diskMock = Substitute.For<IFileSystem>();

    public EncryptedAssetViewTests()
    {
        byte[] encrypted = [.. Plain];
        EncryptedAssets.Decrypt(encrypted, 0, AssetKeyStart);
        _diskMock.FileExists(AssetFile).Returns(true);
        _diskMock.ReadAllBytes(AssetFile).Returns(_ => encrypted.ToArray());
        _diskMock.ReadBytes(AssetFile, 5, 7).Returns(_ => encrypted.AsSpan(5, 7).ToArray());
    }

    public EncryptedAssetView CreateEncryptedAssetView() => new(_diskMock, [AssetFolder]);

    [Fact]
    public void FileExists_DataPathOfAnEncryptedFile_ReturnsTrue()
    {
        // Act
        bool exists = CreateEncryptedAssetView().FileExists(DataView);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void FileExists_DataPathWithoutAnEncryptedFile_ReturnsFalse()
    {
        // Act
        bool exists = CreateEncryptedAssetView().FileExists(AssetFolder + @"\char\char_xyz_pal.pac");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void ReadAllBytes_DataPathOfAnEncryptedFile_ReturnsTheDecryptedBytes()
    {
        // Act
        byte[] bytes = CreateEncryptedAssetView().ReadAllBytes(DataView);

        // Assert
        Assert.Equal(Plain, bytes);
    }

    [Fact]
    public void ReadBytes_PartOfAnEncryptedFile_DecryptsAtThatOffset()
    {
        // Act
        byte[] bytes = CreateEncryptedAssetView().ReadBytes(DataView, 5, 7);

        // Assert
        Assert.Equal(Plain.AsSpan(5, 7).ToArray(), bytes);
    }

    [Fact]
    public void OpenRead_DataPathOfAnEncryptedFile_StreamsTheDecryptedBytes()
    {
        // Arrange
        using Stream stream = CreateEncryptedAssetView().OpenRead(DataView);
        using MemoryStream copy = new();

        // Act
        stream.CopyTo(copy);

        // Assert
        Assert.Equal(Plain, copy.ToArray());
    }

    [Fact]
    public void ReadAllBytes_TheAssetFileItself_ReturnsItAsItIs()
    {
        // Arrange
        byte[] encrypted = _diskMock.ReadAllBytes(AssetFile);

        // Act
        byte[] bytes = CreateEncryptedAssetView().ReadAllBytes(AssetFile);

        // Assert
        Assert.Equal(encrypted, bytes);
    }

    [Fact]
    public void ReadAllBytes_PathOutsideEveryAssetFolder_ReadsTheDisk()
    {
        // Arrange
        _diskMock.ReadAllBytes(@"D:\BBTAG\data\char\char_ahe_pal.pac").Returns([1, 2]);

        // Act
        byte[] bytes = CreateEncryptedAssetView().ReadAllBytes(@"D:\BBTAG\data\char\char_ahe_pal.pac");

        // Assert
        Assert.Equal([1, 2], bytes);
    }

    [Fact]
    public void DirectoryExists_FolderInsideTheDataView_ReturnsFalse()
    {
        // Arrange
        _diskMock.DirectoryExists(AssetFolder + @"\char").Returns(true);

        // Act
        bool exists = CreateEncryptedAssetView().DirectoryExists(AssetFolder + @"\char");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void EnumerateFiles_FolderInsideTheDataView_ReturnsNothing()
    {
        // Act
        IEnumerable<string> files = CreateEncryptedAssetView().EnumerateFiles(AssetFolder + @"\char");

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void DirectoryExists_TheAssetFolderItself_AsksTheDisk()
    {
        // Arrange
        _diskMock.DirectoryExists(AssetFolder).Returns(true);

        // Act
        bool exists = CreateEncryptedAssetView().DirectoryExists(AssetFolder);

        // Assert
        Assert.True(exists);
    }
}
