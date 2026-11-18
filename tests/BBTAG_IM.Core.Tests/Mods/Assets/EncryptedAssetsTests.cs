using BBTAG_IM.Core.Mods.Assets;

namespace BBTAG_IM.Core.Tests.Mods.Assets;

public class EncryptedAssetsTests
{
    private const string Key = @"data\char\char_ahe_pal.pac";
    private const string AssetName = "ac182c18ad5957273f33d5cc0ddd4fe8";
    private const int AssetKeyStart = 26;

    [Theory]
    [InlineData(Key)]
    [InlineData("data/char/char_ahe_pal.pac")]
    [InlineData(@"DATA\Char\CHAR_AHE_PAL.pac")]
    public void NameOf_GameFileKey_ReturnsTheMd5OfTheLowerCaseSlashPath(string key)
    {
        // Act
        string name = EncryptedAssets.NameOf(key);

        // Assert
        Assert.Equal(AssetName, name);
    }

    [Theory]
    [InlineData(@"asset\" + AssetName)]
    [InlineData(@"ASSET\" + AssetName)]
    public void AssetNameIn_KeyOfTheAssetFolder_ReturnsTheAssetName(string key)
    {
        // Act
        string? name = EncryptedAssets.AssetNameIn(key);

        // Assert
        Assert.Equal(AssetName, name, ignoreCase: true);
    }

    [Theory]
    [InlineData(Key)]
    [InlineData(@"asset\readme.txt")]
    [InlineData(@"asset\sub\" + AssetName)]
    [InlineData(@"data\asset\" + AssetName)]
    public void AssetNameIn_OtherKey_ReturnsNull(string key)
    {
        // Act
        string? name = EncryptedAssets.AssetNameIn(key);

        // Assert
        Assert.Null(name);
    }

    [Theory]
    [InlineData(AssetName)]
    [InlineData("AC182C18AD5957273F33D5CC0DDD4FE8")]
    public void IsAssetName_Md5HexName_ReturnsTrue(string fileName)
    {
        // Act
        bool isAsset = EncryptedAssets.IsAssetName(fileName);

        // Assert
        Assert.True(isAsset);
    }

    [Theory]
    [InlineData("readme.txt")]
    [InlineData(AssetName + ".bak")]
    [InlineData("zc182c18ad5957273f33d5cc0ddd4fe8")]
    [InlineData("ac182c18ad5957273f33d5cc0ddd4fe")]
    public void IsAssetName_OtherName_ReturnsFalse(string fileName)
    {
        // Act
        bool isAsset = EncryptedAssets.IsAssetName(fileName);

        // Assert
        Assert.False(isAsset);
    }

    [Theory]
    [InlineData(AssetName)]
    [InlineData("AC182C18AD5957273F33D5CC0DDD4FE8")]
    public void KeyStartOf_AssetName_ReturnsTheEighthByteOfItsMd5ModuloTheKeyLength(string assetName)
    {
        // Act
        int keyStart = EncryptedAssets.KeyStartOf(assetName);

        // Assert
        Assert.Equal(AssetKeyStart, keyStart);
    }

    [Fact]
    public void Decrypt_StartOfAnAsset_RestoresTheArchiveHeader()
    {
        // Arrange
        byte[] bytes = Convert.FromHexString("943D7784E173DFA1");

        // Act
        EncryptedAssets.Decrypt(bytes, 0, AssetKeyStart);

        // Assert
        Assert.Equal(Convert.FromHexString("4650414320150000"), bytes);
    }

    [Fact]
    public void Decrypt_BytesReadFromTheMiddleOfAnAsset_UsesTheKeyAtThatOffset()
    {
        // Arrange
        byte[] bytes = Convert.FromHexString("4BB75D0698F151F1");

        // Act
        EncryptedAssets.Decrypt(bytes, 0x101, AssetKeyStart);

        // Assert
        Assert.Equal(Convert.FromHexString("686530305F30372E"), bytes);
    }

    [Fact]
    public void Decrypt_OffsetPastFourGigabytes_KeepsTheKeyPosition()
    {
        // Arrange
        long offset = 0x101 + 43L * 100_000_000;
        byte[] bytes = Convert.FromHexString("4BB75D0698F151F1");

        // Act
        EncryptedAssets.Decrypt(bytes, offset, AssetKeyStart);

        // Assert
        Assert.Equal(Convert.FromHexString("686530305F30372E"), bytes);
    }
}
