using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Tests.Palettes;

public class PaletteGradientTests
{
    private static readonly PaletteColor Black = new(0, 0, 0, 255);
    private static readonly PaletteColor White = new(255, 255, 255, 255);

    [Fact]
    public void Between_NoEntries_ReturnsNoColors()
    {
        // Act
        PaletteColor[] colors = PaletteGradient.Between(Black, White, 0);

        // Assert
        Assert.Empty(colors);
    }

    [Fact]
    public void Between_OneEntry_ReturnsTheFirstColor()
    {
        // Act
        PaletteColor[] colors = PaletteGradient.Between(Black, White, 1);

        // Assert
        Assert.Equal([Black], colors);
    }

    [Fact]
    public void Between_TwoEntries_ReturnsBothEnds()
    {
        // Act
        PaletteColor[] colors = PaletteGradient.Between(Black, White, 2);

        // Assert
        Assert.Equal([Black, White], colors);
    }

    [Fact]
    public void Between_FiveEntries_StepsEvenlyAndRounds()
    {
        // Act
        PaletteColor[] colors = PaletteGradient.Between(Black, White, 5);

        // Assert
        Assert.Equal(
            [Black, new PaletteColor(64, 64, 64, 255), new PaletteColor(128, 128, 128, 255), new PaletteColor(191, 191, 191, 255), White],
            colors);
    }

    [Fact]
    public void Between_EachChannel_BlendsOnItsOwn()
    {
        // Arrange
        PaletteColor from = new(200, 0, 100, 0);
        PaletteColor to = new(0, 100, 100, 200);

        // Act
        PaletteColor[] colors = PaletteGradient.Between(from, to, 3);

        // Assert
        Assert.Equal(new PaletteColor(100, 50, 100, 100), colors[1]);
    }
}
