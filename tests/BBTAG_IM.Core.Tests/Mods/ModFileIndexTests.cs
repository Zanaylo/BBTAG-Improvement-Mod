using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModFileIndexTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    private ModFolder ModWithFiles(string name, string dataFolder, params string[] relativeFiles)
    {
        var root = $@"D:\Mods\{name}";
        var dataRoot = Path.Combine(root, dataFolder);
        _fileSystem.EnumerateFilesRecursively(dataRoot).Returns(relativeFiles.Select(file => Path.Combine(dataRoot, file)).ToArray());
        return new ModFolder(name, root, [new ModDataRoot(dataRoot, "data")], null);
    }

    [Fact]
    public void Resolve_ReturnsModFileForGameDataKey()
    {
        var mod = ModWithFiles("Overhaul", "data2", @"char\char_adh_col.pac");

        var index = ModFileIndex.Build([mod], _fileSystem);

        Assert.Equal(@"D:\Mods\Overhaul\data2\char\char_adh_col.pac", index.Resolve(@"data\char\char_adh_col.pac"));
    }

    [Fact]
    public void Resolve_IgnoresCase()
    {
        var mod = ModWithFiles("Overhaul", "data2", @"Char\Char_ADH_col.pac");

        var index = ModFileIndex.Build([mod], _fileSystem);

        Assert.Equal(@"D:\Mods\Overhaul\data2\Char\Char_ADH_col.pac", index.Resolve(@"DATA\char\char_adh_COL.pac"));
    }

    [Fact]
    public void Resolve_ReturnsNullForFileNoModProvides()
    {
        var mod = ModWithFiles("Overhaul", "data2", @"char\char_adh_col.pac");

        var index = ModFileIndex.Build([mod], _fileSystem);

        Assert.Null(index.Resolve(@"data\char\char_adh_pal.pac"));
    }

    [Fact]
    public void Build_GivesEarlierModsPriority()
    {
        var first = ModWithFiles("First", "data", @"bg\bg_riverbed.pac");
        var second = ModWithFiles("Second", "data2", @"bg\bg_riverbed.pac", @"bg\bg_exultimate.pac");

        var index = ModFileIndex.Build([first, second], _fileSystem);

        Assert.Equal(@"D:\Mods\First\data\bg\bg_riverbed.pac", index.Resolve(@"data\bg\bg_riverbed.pac"));
        Assert.Equal(@"D:\Mods\Second\data2\bg\bg_exultimate.pac", index.Resolve(@"data\bg\bg_exultimate.pac"));
        Assert.Equal(2, index.Count);
        Assert.Equal(1, index.FilesByMod["First"]);
        Assert.Equal(1, index.FilesByMod["Second"]);
    }

    [Fact]
    public void Build_SkipsModsWithoutDataRoots()
    {
        var executableOnly = new ModFolder("ExeOnly", @"D:\Mods\ExeOnly", [], @"D:\Mods\ExeOnly\BBTAG.exe");

        var index = ModFileIndex.Build([executableOnly], _fileSystem);

        Assert.Equal(0, index.Count);
        Assert.Equal(0, index.FilesByMod["ExeOnly"]);
    }
}
