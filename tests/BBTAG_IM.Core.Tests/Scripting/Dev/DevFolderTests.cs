using BBTAG_IM.Core.Scripting;
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
        ScriptPaths paths = dev.Script("brg", effects: true, ScriptLanguage.Python);

        // Assert
        Assert.Equal(
            new ScriptPaths(@"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brgea.py", @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brgea.bin"),
            paths);
    }
    [Fact]
    public void Script_NoLanguageGiven_IsACSharpFile()
    {
        // Arrange
        DevFolder dev = DevFolder.ForMod(ModFolder);

        // Act
        ScriptPaths paths = dev.Script("brg", effects: false);

        // Assert
        Assert.Equal(@"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.cs", paths.Source);
    }

    [Fact]
    public void CollisionFile_SpriteOfACharacter_PointsIntoTheLooseCollisionArchive()
    {
        // Arrange
        DevFolder dev = DevFolder.ForMod(ModFolder);

        // Act
        string path = dev.CollisionFile("brg", "rg000_04.jonbin");

        // Assert
        Assert.Equal(@"D:\BBTAG\mods\DEV\data\char\char_brg_col\rg000_04.jonbin", path);
    }
}
