using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModFolderTests
{
    private const string Root = @"D:\Mods\BBTAG-OverHaul-Patches";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public ModFolderTests()
    {
        _fileSystemMock.DirectoryExists(Root).Returns(true);
    }

    [Fact]
    public void Inspect_FolderWithData2_MountsItAsGameData()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(Path.Combine(Root, "data2")).Returns(true);

        // Act
        ModFolder mod = ModFolder.Inspect(Root, _fileSystemMock);

        // Assert
        Assert.Equal([new ModDataRoot(Path.Combine(Root, "data2"), "data")], mod.DataRoots);
    }

    [Fact]
    public void Inspect_FolderWithDataAndData2_MountsDataFirst()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(Path.Combine(Root, "data")).Returns(true);
        _fileSystemMock.DirectoryExists(Path.Combine(Root, "data2")).Returns(true);

        // Act
        ModFolder mod = ModFolder.Inspect(Root, _fileSystemMock);

        // Assert
        Assert.Equal(
            [new ModDataRoot(Path.Combine(Root, "data"), "data"), new ModDataRoot(Path.Combine(Root, "data2"), "data")],
            mod.DataRoots);
    }

    [Fact]
    public void Inspect_FolderWithGameExecutable_FindsTheModdedExecutable()
    {
        // Arrange
        _fileSystemMock.FileExists(Path.Combine(Root, "BBTAG.exe")).Returns(true);

        // Act
        ModFolder mod = ModFolder.Inspect(Root, _fileSystemMock);

        // Assert
        Assert.Equal(Path.Combine(Root, "BBTAG.exe"), mod.ExecutablePath);
    }

    [Fact]
    public void Inspect_FolderWithOneTable_FindsTheTable()
    {
        // Arrange
        _fileSystemMock.EnumerateFiles(Root).Returns([Path.Combine(Root, "readme.txt"), Path.Combine(Root, "Overhaul.CT")]);

        // Act
        ModFolder mod = ModFolder.Inspect(Root, _fileSystemMock);

        // Assert
        Assert.Equal(Path.Combine(Root, "Overhaul.CT"), mod.TablePath);
    }

    [Fact]
    public void Inspect_SeveralTablesOneNamedAfterTheGame_PrefersThatTable()
    {
        // Arrange
        _fileSystemMock.EnumerateFiles(Root).Returns([Path.Combine(Root, "Alpha.CT"), Path.Combine(Root, "bbtag.ct"), Path.Combine(Root, "Zeta.ct")]);

        // Act
        ModFolder mod = ModFolder.Inspect(Root, _fileSystemMock);

        // Assert
        Assert.Equal(Path.Combine(Root, "bbtag.ct"), mod.TablePath);
    }

    [Fact]
    public void Inspect_SeveralTablesNoneNamedAfterTheGame_PicksTheFirstByName()
    {
        // Arrange
        _fileSystemMock.EnumerateFiles(Root).Returns([Path.Combine(Root, "Zeta.CT"), Path.Combine(Root, "Alpha.CT")]);

        // Act
        ModFolder mod = ModFolder.Inspect(Root, _fileSystemMock);

        // Assert
        Assert.Equal(Path.Combine(Root, "Alpha.CT"), mod.TablePath);
    }

    [Fact]
    public void Inspect_FolderWithDllFiles_ListsThemAsPluginsInNameOrder()
    {
        // Arrange
        _fileSystemMock.EnumerateFiles(Root).Returns(
            [Path.Combine(Root, "Zeta.dll"), Path.Combine(Root, "readme.txt"), Path.Combine(Root, "ImprovementMod.DLL")]);

        // Act
        ModFolder mod = ModFolder.Inspect(Root, _fileSystemMock);

        // Assert
        Assert.Equal([Path.Combine(Root, "ImprovementMod.DLL"), Path.Combine(Root, "Zeta.dll")], mod.PluginPaths);
    }

    [Fact]
    public void Inspect_FolderWithOnlyAPlugin_IsAValidMod()
    {
        // Arrange
        _fileSystemMock.EnumerateFiles(Root).Returns([Path.Combine(Root, "ImprovementMod.dll")]);

        // Act
        Exception? exception = Record.Exception(() => ModFolder.Inspect(Root, _fileSystemMock));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Inspect_FolderWithOnlyATable_IsAValidMod()
    {
        // Arrange
        _fileSystemMock.EnumerateFiles(Root).Returns([Path.Combine(Root, "BBTAG.CT")]);

        // Act
        Exception? exception = Record.Exception(() => ModFolder.Inspect(Root, _fileSystemMock));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Inspect_PathWithTrailingSeparator_UsesTheFolderNameAndTrimmedRoot()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(Root + @"\").Returns(true);
        _fileSystemMock.DirectoryExists(Path.Combine(Root, "data2")).Returns(true);

        // Act
        ModFolder mod = ModFolder.Inspect(Root + @"\", _fileSystemMock);

        // Assert
        Assert.Equal(("BBTAG-OverHaul-Patches", Root), (mod.Name, mod.RootPath));
    }

    [Fact]
    public void Inspect_MissingFolder_ThrowsInvalidModFolder()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(Root).Returns(false);

        // Act
        Exception? exception = Record.Exception(() => ModFolder.Inspect(Root, _fileSystemMock));

        // Assert
        Assert.IsType<InvalidModFolderException>(exception);
    }

    [Fact]
    public void Inspect_FolderWithNothingToLoad_ThrowsInvalidModFolder()
    {
        // Arrange
        _fileSystemMock.EnumerateFiles(Root).Returns([Path.Combine(Root, "readme.txt")]);

        // Act
        Exception? exception = Record.Exception(() => ModFolder.Inspect(Root, _fileSystemMock));

        // Assert
        Assert.IsType<InvalidModFolderException>(exception);
    }
}
