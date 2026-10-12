using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModContentsTests
{
    private const string Root = @"D:\BBTAG\mods\BBTAG-OverHaul";

    [Fact]
    public void Describe_ModWithEveryKindOfContent_ListsEachPart()
    {
        // Arrange
        ModFolder mod = new(
            "BBTAG-OverHaul",
            Root,
            [new ModDataRoot(Root + @"\data", "data"), new ModDataRoot(Root + @"\data2", "data")],
            Root + @"\BBTAG.exe",
            Root + @"\BBTAG.CT",
            [Root + @"\ImprovementMod.dll"]);

        // Act
        string contents = ModContents.Describe(mod, IsPlugin);

        // Assert
        Assert.Equal("data folder, data2 folder, BBTAG.exe, Cheat Engine table BBTAG.CT, plugin ImprovementMod.dll", contents);
    }

    [Fact]
    public void Describe_ModWithOnlyAPlugin_ListsThePlugin()
    {
        // Arrange
        ModFolder mod = new("BBTAG-Improvement-Mod", Root, [], null, null, [Root + @"\ImprovementMod.dll"]);

        // Act
        string contents = ModContents.Describe(mod, IsPlugin);

        // Assert
        Assert.Equal("plugin ImprovementMod.dll", contents);
    }

    [Fact]
    public void Describe_DllThatIsNotAPlugin_ListsItAsALibrary()
    {
        // Arrange
        ModFolder mod = new("BBTAG-Improvement-Mod", Root, [], null, null, [Root + @"\cimgui.dll", Root + @"\ImprovementMod.dll"]);

        // Act
        string contents = ModContents.Describe(mod, IsPlugin);

        // Assert
        Assert.Equal("library cimgui.dll, plugin ImprovementMod.dll", contents);
    }

    private static bool IsPlugin(string path) => path.EndsWith("ImprovementMod.dll", StringComparison.Ordinal);
}
