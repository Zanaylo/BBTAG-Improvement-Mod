using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Tests.Mods;

public class CharacterFileAliasesTests
{
    private static CharacterManifest Manifest(string code, string? filesFrom) =>
        new(code, "brg", "NAME", null, Series.Others, new CharacterNames("Name", "NAME", "NAME"), filesFrom);

    [Fact]
    public void AliasOf_FileOfTheCharacter_NamesTheFileOfItsSource()
    {
        // Arrange
        CharacterFileAliases aliases = new([Manifest("xm4", "uhy")]);

        // Act
        string? alias = aliases.AliasOf(@"data\sound\se\seXM4.pac");

        // Assert
        Assert.Equal(@"data\sound\se\seuhy.pac", alias);
    }

    [Fact]
    public void AliasOf_OtherFile_HasNone()
    {
        // Arrange
        CharacterFileAliases aliases = new([Manifest("xm4", "uhy")]);

        // Act
        string? alias = aliases.AliasOf(@"data\char\char_brg_scr.pac");

        // Assert
        Assert.Null(alias);
    }

    [Fact]
    public void AliasOf_CharacterWithoutASource_HasNone()
    {
        // Arrange
        CharacterFileAliases aliases = new([Manifest("rar", null)]);

        // Act
        string? alias = aliases.AliasOf(@"data\char\char_rar_scr.pac");

        // Assert
        Assert.Null(alias);
    }
}
