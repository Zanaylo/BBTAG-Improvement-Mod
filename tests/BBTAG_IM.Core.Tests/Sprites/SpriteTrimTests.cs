using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Sprites;

public class SpriteTrimTests
{
    [Fact]
    public void Trim_ClearBorder_CutsItAndMovesThePlace()
    {
        // Arrange
        byte[] indices =
        [
            0, 0, 0, 0,
            0, 5, 6, 0,
            0, 0, 7, 0,
        ];
        PlacedSprite sprite = new(new IndexedSprite(4, 3, indices), 10, 20, []);

        // Act
        PlacedSprite trimmed = SpriteTrim.Trim(sprite);

        // Assert
        Assert.Equal((11, 21, 2, 2), (trimmed.X, trimmed.Y, trimmed.Image.Width, trimmed.Image.Height));
        Assert.Equal([5, 6, 0, 7], trimmed.Image.Indices);
    }

    [Fact]
    public void Trim_NothingToSee_KeepsTheSprite()
    {
        // Arrange
        PlacedSprite sprite = new(new IndexedSprite(2, 1, [0, 0]), 10, 20, []);

        // Act
        PlacedSprite trimmed = SpriteTrim.Trim(sprite);

        // Assert
        Assert.Same(sprite, trimmed);
    }

    [Fact]
    public void Trim_NoClearBorder_KeepsTheSprite()
    {
        // Arrange
        PlacedSprite sprite = new(new IndexedSprite(2, 1, [3, 4]), 10, 20, []);

        // Act
        PlacedSprite trimmed = SpriteTrim.Trim(sprite);

        // Assert
        Assert.Same(sprite, trimmed);
    }
}
