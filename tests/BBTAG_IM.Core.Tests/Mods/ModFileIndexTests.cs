using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModFileIndexTests
{
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

    private ModFolder ModWithFiles(string name, string dataFolder, params string[] relativeFiles)
    {
        string root = $@"D:\Mods\{name}";
        string dataRoot = Path.Combine(root, dataFolder);
        _fileSystemMock.EnumerateFilesRecursively(dataRoot).Returns(relativeFiles.Select(file => Path.Combine(dataRoot, file)).ToArray());
        return new ModFolder(name, root, [new ModDataRoot(dataRoot, "data")], null, null, []);
    }
}
