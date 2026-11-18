using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModFileIndexTests
{
    private const string PaletteKey = @"data\char\char_ahe_pal.pac";
    private const string PaletteAsset = "ac182c18ad5957273f33d5cc0ddd4fe8";
    private const int PaletteKeyStart = 26;

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Resolve_GameDataKeyAModProvides_ReturnsTheModFile()
    {
        // Arrange
        ModFolder mod = ModWithFiles("Overhaul", "data2", @"char\char_adh_col.pac");
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"data\char\char_adh_col.pac");

        // Assert
        Assert.Equal(@"D:\Mods\Overhaul\data2\char\char_adh_col.pac", resolved);
    }

    [Fact]
    public void Resolve_KeyInAnotherCase_ReturnsTheModFile()
    {
        // Arrange
        ModFolder mod = ModWithFiles("Overhaul", "data2", @"Char\Char_ADH_col.pac");
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"DATA\char\char_adh_COL.pac");

        // Assert
        Assert.Equal(@"D:\Mods\Overhaul\data2\Char\Char_ADH_col.pac", resolved);
    }

    [Fact]
    public void Resolve_FileNoModProvides_ReturnsNull()
    {
        // Arrange
        ModFolder mod = ModWithFiles("Overhaul", "data2", @"char\char_adh_col.pac");
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"data\char\char_adh_pal.pac");

        // Assert
        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_TwoModsProvideTheSameFile_ReturnsTheFileOfTheFirstMod()
    {
        // Arrange
        ModFolder first = ModWithFiles("First", "data", @"bg\bg_riverbed.pac");
        ModFolder second = ModWithFiles("Second", "data2", @"bg\bg_riverbed.pac", @"bg\bg_exultimate.pac");
        ModFileIndex index = ModFileIndex.Build([first, second], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"data\bg\bg_riverbed.pac");

        // Assert
        Assert.Equal(@"D:\Mods\First\data\bg\bg_riverbed.pac", resolved);
    }

    [Fact]
    public void Build_TwoModsProvideTheSameFile_CountsItOnlyForTheFirstMod()
    {
        // Arrange
        ModFolder first = ModWithFiles("First", "data", @"bg\bg_riverbed.pac");
        ModFolder second = ModWithFiles("Second", "data2", @"bg\bg_riverbed.pac", @"bg\bg_exultimate.pac");

        // Act
        ModFileIndex index = ModFileIndex.Build([first, second], _fileSystemMock);

        // Assert
        Assert.Equal((2, 1, 1), (index.Count, index.FilesByMod["First"], index.FilesByMod["Second"]));
    }

    [Fact]
    public void Build_ModWithoutDataFolders_AddsNoFiles()
    {
        // Arrange
        ModFolder executableOnly = new("ExeOnly", @"D:\Mods\ExeOnly", [], @"D:\Mods\ExeOnly\BBTAG.exe", null, []);

        // Act
        ModFileIndex index = ModFileIndex.Build([executableOnly], _fileSystemMock);

        // Assert
        Assert.Equal((0, 0), (index.Count, index.FilesByMod["ExeOnly"]));
    }

    [Fact]
    public void Resolve_KeyAModShipsOnlyAsAnEncryptedAsset_ReturnsTheAssetFile()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", PaletteAsset);
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(PaletteKey);

        // Assert
        Assert.Equal(@"D:\Mods\Unlimited\asset\" + PaletteAsset, resolved);
    }

    [Fact]
    public void Resolve_KeyInAnotherCaseShippedAsAnEncryptedAsset_ReturnsTheAssetFile()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", PaletteAsset.ToUpperInvariant());
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"DATA\Char\CHAR_AHE_PAL.pac");

        // Assert
        Assert.Equal(@"D:\Mods\Unlimited\asset\" + PaletteAsset.ToUpperInvariant(), resolved);
    }

    [Fact]
    public void Resolve_ModShipsTheKeyDecryptedAndEncrypted_ReturnsTheDecryptedFile()
    {
        // Arrange
        ModFolder mod = ModWithFiles("Unlimited", "data", @"char\char_ahe_pal.pac") with { EncryptedAssetsPath = @"D:\Mods\Unlimited\asset" };
        _fileSystemMock.EnumerateFiles(@"D:\Mods\Unlimited\asset").Returns([@"D:\Mods\Unlimited\asset\" + PaletteAsset]);
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(PaletteKey);

        // Assert
        Assert.Equal(@"D:\Mods\Unlimited\data\char\char_ahe_pal.pac", resolved);
    }

    [Fact]
    public void Resolve_FirstModShipsTheKeyEncrypted_ReturnsTheFirstModFile()
    {
        // Arrange
        ModFolder first = ModWithAssets("Unlimited", PaletteAsset);
        ModFolder second = ModWithFiles("Overhaul", "data", @"char\char_ahe_pal.pac");
        ModFileIndex index = ModFileIndex.Build([first, second], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(PaletteKey);

        // Assert
        Assert.Equal(@"D:\Mods\Unlimited\asset\" + PaletteAsset, resolved);
    }

    [Fact]
    public void Resolve_FirstModShipsTheKeyDecrypted_ReturnsTheFirstModFile()
    {
        // Arrange
        ModFolder first = ModWithFiles("Overhaul", "data", @"char\char_ahe_pal.pac");
        ModFolder second = ModWithAssets("Unlimited", PaletteAsset);
        ModFileIndex index = ModFileIndex.Build([first, second], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(PaletteKey);

        // Assert
        Assert.Equal(@"D:\Mods\Overhaul\data\char\char_ahe_pal.pac", resolved);
    }

    [Fact]
    public void Resolve_KeyOutsideTheDataFolder_ReturnsNull()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", "1e46750ec5914ee43b275bbbc3ff12a2");
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"save\settings.dat");

        // Assert
        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_AssetKeyOfAFileAModShipsDecrypted_ReturnsTheDecryptedFile()
    {
        // Arrange
        ModFolder mod = ModWithFiles("Overhaul", "data", @"char\char_ahe_pal.pac");
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"asset\" + PaletteAsset);

        // Assert
        Assert.Equal(@"D:\Mods\Overhaul\data\char\char_ahe_pal.pac", resolved);
    }

    [Fact]
    public void Resolve_AssetKeyOfAModAsset_ReturnsTheAssetFile()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", PaletteAsset);
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"ASSET\" + PaletteAsset.ToUpperInvariant());

        // Assert
        Assert.Equal(@"D:\Mods\Unlimited\asset\" + PaletteAsset, resolved);
    }

    [Fact]
    public void Resolve_AssetKeyShippedDecryptedByTheFirstModAndEncryptedByTheSecond_ReturnsTheFirstModFile()
    {
        // Arrange
        ModFolder first = ModWithFiles("Overhaul", "data", @"char\char_ahe_pal.pac");
        ModFolder second = ModWithAssets("Unlimited", PaletteAsset);
        ModFileIndex index = ModFileIndex.Build([first, second], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"asset\" + PaletteAsset);

        // Assert
        Assert.Equal(@"D:\Mods\Overhaul\data\char\char_ahe_pal.pac", resolved);
    }

    [Fact]
    public void Resolve_AssetKeyNoModShips_ReturnsNull()
    {
        // Arrange
        ModFolder mod = ModWithFiles("Overhaul", "data", @"char\char_adh_col.pac");
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.Resolve(@"asset\" + PaletteAsset);

        // Assert
        Assert.Null(resolved);
    }

    [Fact]
    public void ResolvePlain_KeyShippedOnlyAsAnEncryptedAsset_ReturnsNull()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", PaletteAsset);
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.ResolvePlain(PaletteKey);

        // Assert
        Assert.Null(resolved);
    }

    [Fact]
    public void ResolvePlain_KeyShippedDecrypted_ReturnsTheModFile()
    {
        // Arrange
        ModFolder mod = ModWithFiles("Overhaul", "data", @"char\char_ahe_pal.pac");
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        string? resolved = index.ResolvePlain(PaletteKey);

        // Assert
        Assert.Equal(@"D:\Mods\Overhaul\data\char\char_ahe_pal.pac", resolved);
    }

    [Fact]
    public void Build_ModWithEncryptedAssets_CountsThemForTheMod()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", PaletteAsset, "fcc0a4c5aa23d8c4807e08617dda78de");

        // Act
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Assert
        Assert.Equal((2, 2), (index.Count, index.FilesByMod["Unlimited"]));
    }

    [Fact]
    public void Build_FileInTheAssetFolderThatIsNotAnAssetName_IsLeftOut()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", "readme.txt", PaletteAsset + ".bak");

        // Act
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Assert
        Assert.Equal(0, index.FilesByMod["Unlimited"]);
    }

    [Fact]
    public void Build_TwoModsShipTheSameEncryptedAsset_CountsItOnlyForTheFirstMod()
    {
        // Arrange
        ModFolder first = ModWithAssets("First", PaletteAsset);
        ModFolder second = ModWithAssets("Second", PaletteAsset);

        // Act
        ModFileIndex index = ModFileIndex.Build([first, second], _fileSystemMock);

        // Assert
        Assert.Equal((1, 1, 0), (index.Count, index.FilesByMod["First"], index.FilesByMod["Second"]));
    }

    [Fact]
    public void AssetKeyStartOf_EncryptedAssetOfAMod_ReturnsItsKeyStart()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", PaletteAsset);
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        int? keyStart = index.AssetKeyStartOf(@"D:\Mods\Unlimited\asset\" + PaletteAsset);

        // Assert
        Assert.Equal(PaletteKeyStart, keyStart);
    }

    [Fact]
    public void AssetKeyStartOf_DecryptedFileOfAMod_ReturnsNull()
    {
        // Arrange
        ModFolder mod = ModWithFiles("Overhaul", "data", @"char\char_ahe_pal.pac");
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        int? keyStart = index.AssetKeyStartOf(@"D:\Mods\Overhaul\data\char\char_ahe_pal.pac");

        // Assert
        Assert.Null(keyStart);
    }

    [Fact]
    public void AssetKeyStartOf_AssetNameOutsideEveryModAssetFolder_ReturnsNull()
    {
        // Arrange
        ModFolder mod = ModWithAssets("Unlimited", PaletteAsset);
        ModFileIndex index = ModFileIndex.Build([mod], _fileSystemMock);

        // Act
        int? keyStart = index.AssetKeyStartOf(@"D:\Mods\Other\asset\" + PaletteAsset);

        // Assert
        Assert.Null(keyStart);
    }

    private ModFolder ModWithFiles(string name, string dataFolder, params string[] relativeFiles)
    {
        string root = $@"D:\Mods\{name}";
        string dataRoot = Path.Combine(root, dataFolder);
        _fileSystemMock.EnumerateFilesRecursively(dataRoot).Returns(relativeFiles.Select(file => Path.Combine(dataRoot, file)).ToArray());
        return new ModFolder(name, root, [new ModDataRoot(dataRoot, "data")], null, null, []);
    }

    private ModFolder ModWithAssets(string name, params string[] assetNames)
    {
        string root = $@"D:\Mods\{name}";
        string assetFolder = Path.Combine(root, "asset");
        _fileSystemMock.EnumerateFiles(assetFolder).Returns(assetNames.Select(asset => Path.Combine(assetFolder, asset)).ToArray());
        return new ModFolder(name, root, [], null, null, [], assetFolder);
    }
}
