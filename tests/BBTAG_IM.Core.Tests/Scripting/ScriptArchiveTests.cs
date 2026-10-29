using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting;

public class ScriptArchiveTests
{
    [Fact]
    public void Scripts_ArchiveWithScriptsAndOtherFiles_ReturnsOnlyTheScripts()
    {
        // Arrange
        byte[] archive = SpriteFiles.Fpac(("scr_brg.bin", [1, 2]), ("readme.txt", [9]), ("scr_brgea.bin", [3]));

        // Act
        IReadOnlyList<ScriptFile> scripts = ScriptArchive.Scripts(archive);

        // Assert
        Assert.Equal(["scr_brg", "scr_brgea"], scripts.Select(script => script.Name));
        Assert.Equal([1, 2], scripts[0].Bytes);
        Assert.Equal([3], scripts[1].Bytes);
    }

    [Theory]
    [InlineData("scr_brg", "brg")]
    [InlineData("scr_brgea", "brg")]
    [InlineData("SCR_BHA", "bha")]
    [InlineData("scr_e0", "e0")]
    [InlineData("scr_e0ea", "e0")]
    public void CharacterCode_ScriptName_IsTheCodeWithoutTheEffectsSuffix(string name, string expected)
    {
        // Act
        string? code = ScriptArchive.CharacterCode(name);

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void CharacterCode_NameThatIsNotACharacterScript_ReturnsNull()
    {
        // Act
        string? code = ScriptArchive.CharacterCode("cmn_brg");

        // Assert
        Assert.Null(code);
    }
}
