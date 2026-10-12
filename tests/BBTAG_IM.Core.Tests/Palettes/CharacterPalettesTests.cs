using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Palettes;

public class CharacterPalettesTests
{
    private const int Aegis = 24;

    public CharacterPalettes CreateCharacterPalettes() =>
        new("Aegis", [ImplPalette.Parse("Minato", ImplFile.Create(Aegis)), ImplPalette.Parse("Shadow Aegis", ImplFile.Create(Aegis))], []);

    [Fact]
    public void Find_NameInOtherCase_ReturnsThePalette()
    {
        // Arrange
        CharacterPalettes palettes = CreateCharacterPalettes();

        // Act
        ImplPalette? palette = palettes.Find("shadow aegis");

        // Assert
        Assert.Equal("Shadow Aegis", palette?.Name);
    }

    [Fact]
    public void Find_UnknownName_ReturnsNull()
    {
        // Arrange
        CharacterPalettes palettes = CreateCharacterPalettes();

        // Act
        ImplPalette? palette = palettes.Find("Gold");

        // Assert
        Assert.Null(palette);
    }
}
