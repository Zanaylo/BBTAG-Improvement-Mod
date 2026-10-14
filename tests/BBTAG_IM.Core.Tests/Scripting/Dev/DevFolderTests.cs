using BBTAG_IM.Core.Scripting.Dev;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class DevFolderTests
{
    private const string ModFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";

    [Fact]
    public void ForMod_ImprovementModFolder_ReturnsTheDevFolderBesideIt()
    {
        // Arrange
        string modFolder = ModFolder;

        // Act
        DevFolder dev = DevFolder.ForMod(modFolder);

        // Assert
        Assert.Equal(@"D:\BBTAG\mods\DEV", dev.Path);
    }

    [Fact]
    public void Script_EffectsOfACharacter_PointsIntoTheLooseScriptArchive()
    {
        // Arrange
        DevFolder dev = DevFolder.ForMod(ModFolder);

        // Act
        ScriptPaths paths = dev.Script("brg", effects: true);

        // Assert
        Assert.Equal(
            new ScriptPaths(@"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brgea.py", @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brgea.bin"),
            paths);
    }
}
