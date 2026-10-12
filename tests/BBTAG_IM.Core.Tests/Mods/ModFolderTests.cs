using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModFolderTests
{
    private const string Root = @"D:\Mods\BBTAG-OverHaul-Patches";

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public ModFolderTests()
    {
        _fileSystem.DirectoryExists(Root).Returns(true);
    }

    [Fact]
    public void Inspect_MountsData2FolderAsGameData()
    {
        _fileSystem.DirectoryExists(Path.Combine(Root, "data2")).Returns(true);

        var mod = ModFolder.Inspect(Root, _fileSystem);

        Assert.Equal("BBTAG-OverHaul-Patches", mod.Name);
        Assert.Equal(Root, mod.RootPath);
        Assert.Equal([new ModDataRoot(Path.Combine(Root, "data2"), "data")], mod.DataRoots);
        Assert.Null(mod.ExecutablePath);
    }

    [Fact]
    public void Inspect_PrefersDataOverData2WhenBothExist()
    {
        _fileSystem.DirectoryExists(Path.Combine(Root, "data")).Returns(true);
        _fileSystem.DirectoryExists(Path.Combine(Root, "data2")).Returns(true);

        var mod = ModFolder.Inspect(Root, _fileSystem);

        Assert.Equal(
            [new ModDataRoot(Path.Combine(Root, "data"), "data"), new ModDataRoot(Path.Combine(Root, "data2"), "data")],
            mod.DataRoots);
    }

    [Fact]
    public void Inspect_FindsModdedExecutable()
    {
        _fileSystem.FileExists(Path.Combine(Root, "BBTAG.exe")).Returns(true);

        var mod = ModFolder.Inspect(Root, _fileSystem);

        Assert.Equal(Path.Combine(Root, "BBTAG.exe"), mod.ExecutablePath);
        Assert.Empty(mod.DataRoots);
    }

    [Fact]
    public void Inspect_TrimsTrailingSeparatorFromName()
    {
        _fileSystem.DirectoryExists(Root + @"\").Returns(true);
        _fileSystem.DirectoryExists(Path.Combine(Root, "data2")).Returns(true);

        var mod = ModFolder.Inspect(Root + @"\", _fileSystem);

        Assert.Equal("BBTAG-OverHaul-Patches", mod.Name);
        Assert.Equal(Root, mod.RootPath);
    }

    [Fact]
    public void Inspect_RejectsMissingFolder()
    {
        _fileSystem.DirectoryExists(Root).Returns(false);

        Assert.Throws<InvalidModFolderException>(() => ModFolder.Inspect(Root, _fileSystem));
    }

    [Fact]
    public void Inspect_RejectsFolderWithoutDataOrExecutable()
    {
        Assert.Throws<InvalidModFolderException>(() => ModFolder.Inspect(Root, _fileSystem));
    }
}
