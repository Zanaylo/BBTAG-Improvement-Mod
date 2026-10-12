using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Tests.Palettes;

public class ColorLabelsTests
{
    [Theory]
    [InlineData(5, "Color 5")]
    [InlineData(99, "Color 99")]
    [InlineData(300, "Color 300")]
    public void Of_Color_NamesTheColorNumber(int color, string expected)
    {
        // Act
        string label = ColorLabels.Of(color);

        // Assert
        Assert.Equal(expected, label);
    }

    [Fact]
    public void Of_SameColorTwice_ReturnsTheSameText()
    {
        // Act
        string first = ColorLabels.Of(7);
        string second = ColorLabels.Of(7);

        // Assert
        Assert.Same(first, second);
    }

    [Theory]
    [InlineData(RandomPalette.Any, ColorLabels.RandomLabel)]
    [InlineData("random_exclude_default", ColorLabels.RandomExceptGameColorsLabel)]
    [InlineData("Minato", "Minato")]
    public void Shown_Assignment_NamesRandomChoicesForPeopleAndPalettesAsTheyAre(string assignment, string expected)
    {
        // Act
        string shown = ColorLabels.Shown(assignment);

        // Assert
        Assert.Equal(expected, shown);
    }
}
