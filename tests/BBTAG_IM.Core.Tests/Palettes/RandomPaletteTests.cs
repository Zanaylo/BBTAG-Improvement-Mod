using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Palettes;

public class RandomPaletteTests
{
    private const int Aegis = 24;

    private static readonly IReadOnlyList<ImplPalette> TwoPalettes =
        [ImplPalette.Parse("Gold", ImplFile.Create(Aegis)), ImplPalette.Parse("Minato", ImplFile.Create(Aegis))];

    private readonly Func<int, int> _pickBelowMock = Substitute.For<Func<int, int>>();

    [Theory]
    [InlineData(RandomPalette.Any, true)]
    [InlineData(RandomPalette.ExceptGameColors, true)]
    [InlineData("random", true)]
    [InlineData("Minato", false)]
    public void IsChoice_Name_RecognisesOnlyTheRandomChoices(string name, bool expected)
    {
        // Act
        bool isChoice = RandomPalette.IsChoice(name);

        // Assert
        Assert.Equal(expected, isChoice);
    }

    [Fact]
    public void Pick_AnyLandingOnAPalette_ReturnsThatPalette()
    {
        // Arrange
        _pickBelowMock(3).Returns(1);

        // Act
        ImplPalette? palette = RandomPalette.Pick(RandomPalette.Any, TwoPalettes, _pickBelowMock);

        // Assert
        Assert.Equal("Minato", palette?.Name);
    }

    [Fact]
    public void Pick_AnyLandingPastThePalettes_ReturnsNullForTheGameColors()
    {
        // Arrange
        _pickBelowMock(3).Returns(2);

        // Act
        ImplPalette? palette = RandomPalette.Pick(RandomPalette.Any, TwoPalettes, _pickBelowMock);

        // Assert
        Assert.Null(palette);
    }

    [Fact]
    public void Pick_ExceptGameColors_PicksAmongThePalettesOnly()
    {
        // Arrange
        _pickBelowMock(2).Returns(0);

        // Act
        ImplPalette? palette = RandomPalette.Pick(RandomPalette.ExceptGameColors, TwoPalettes, _pickBelowMock);

        // Assert
        Assert.Equal("Gold", palette?.Name);
    }

    [Fact]
    public void Pick_ExceptGameColorsWithoutPalettes_ReturnsNullWithoutPicking()
    {
        // Act
        ImplPalette? palette = RandomPalette.Pick(RandomPalette.ExceptGameColors, [], _pickBelowMock);

        // Assert
        Assert.Null(palette);
        _pickBelowMock.DidNotReceive()(Arg.Any<int>());
    }

    [Fact]
    public void Pick_NameThatIsNotARandomChoice_ThrowsArgumentException()
    {
        // Act
        Exception? exception = Record.Exception(() => RandomPalette.Pick("Minato", TwoPalettes, _pickBelowMock));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }
}
