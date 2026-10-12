using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Tests.Palettes;

public class CharactersTests
{
    [Theory]
    [InlineData(0, "Ragna")]
    [InlineData(24, "Aegis")]
    [InlineData(40, "NaotoKurogane")]
    [InlineData(52, "Blitztank")]
    public void NameOf_IndexOfTheGame_ReturnsThePaletteFolderName(int index, string expected)
    {
        // Act
        string? name = Characters.NameOf(index);

        // Assert
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(53)]
    [InlineData(1000)]
    public void NameOf_IndexWithoutPalettes_ReturnsNull(int index)
    {
        // Act
        string? name = Characters.NameOf(index);

        // Assert
        Assert.Null(name);
    }

    [Fact]
    public void IndexOf_NameInOtherCase_ReturnsTheIndex()
    {
        // Act
        int? index = Characters.IndexOf("aegis");

        // Assert
        Assert.Equal(24, index);
    }

    [Fact]
    public void IndexOf_UnknownName_ReturnsNull()
    {
        // Act
        int? index = Characters.IndexOf("Boss");

        // Assert
        Assert.Null(index);
    }

    [Theory]
    [InlineData("mko", true)]
    [InlineData("pmr1", true)]
    [InlineData("g", true)]
    [InlineData("Packs", false)]
    [InlineData("MKO", false)]
    [InlineData("", false)]
    [InlineData("m-k", false)]
    public void IsCode_Text_AcceptsOnlyShortLowercaseCharacterCodes(string text, bool expected)
    {
        // Act
        bool isCode = Characters.IsCode(text);

        // Assert
        Assert.Equal(expected, isCode);
    }
}
