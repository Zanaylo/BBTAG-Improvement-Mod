using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Tests.Overlay;

public class MenuFontTests
{
    [Fact]
    public void SizeFor_NoChoice_GrowsWithTheScreen()
    {
        // Arrange
        float screenHeight = 1080;

        // Act
        float size = MenuFont.SizeFor(null, screenHeight);

        // Assert
        Assert.Equal(22, size);
    }

    [Fact]
    public void SizeFor_NoChoiceOnASmallScreen_KeepsTheTextReadable()
    {
        // Arrange
        float screenHeight = 600;

        // Act
        float size = MenuFont.SizeFor(null, screenHeight);

        // Assert
        Assert.Equal(16, size);
    }

    [Fact]
    public void SizeFor_Choice_UsesIt()
    {
        // Arrange
        float chosen = 13;

        // Act
        float size = MenuFont.SizeFor(chosen, 1080);

        // Assert
        Assert.Equal(13, size);
    }

    [Theory]
    [InlineData(4f, MenuFont.Smallest)]
    [InlineData(60f, MenuFont.Largest)]
    public void SizeFor_ChoiceOutOfRange_KeepsItInRange(float chosen, float expected)
    {
        // Arrange
        float screenHeight = 1080;

        // Act
        float size = MenuFont.SizeFor(chosen, screenHeight);

        // Assert
        Assert.Equal(expected, size);
    }

    [Fact]
    public void SizeFor_HalfPixelChoice_RoundsToWholePixels()
    {
        // Arrange
        float chosen = 14.6f;

        // Act
        float size = MenuFont.SizeFor(chosen, 1080);

        // Assert
        Assert.Equal(15, size);
    }
}
