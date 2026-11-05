using System.Numerics;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class ChunkPlacementTests
{
    private const int Precision = 4;

    [Fact]
    public void Place_PieceInsideTheImage_DrawsAllOfIt()
    {
        // Arrange
        PlacedSprite sprite = Sprite(32, 210, 412, 342);
        SpriteChunk chunk = new(32, 210, 412, 342, -403, -345, 412, 342, 0, 0);

        // Act
        PlacedChunk? placed = ChunkPlacement.Place(chunk, sprite);

        // Assert
        Assert.NotNull(placed);
        Assert.Equal((-403f, -345f, 412f, 342f), (placed.X, placed.Y, placed.Width, placed.Height));
        Assert.Equal(Vector2.Zero, placed.UvMin);
        Assert.Equal(Vector2.One, placed.UvMax);
    }

    [Fact]
    public void Place_PieceLargerThanTheImage_KeepsOnlyThePartWithPixels()
    {
        // Arrange
        PlacedSprite sprite = Sprite(5, 0, 508, 488);
        SpriteChunk chunk = new(0, 23, 511, 489, -256, -449, 511, 489, 0, 0);

        // Act
        PlacedChunk? placed = ChunkPlacement.Place(chunk, sprite);

        // Assert
        Assert.NotNull(placed);
        Assert.Equal((-251f, -449f, 506f, 465f), (placed.X, placed.Y, placed.Width, placed.Height));
        Assert.Equal(0, placed.UvMin.X, Precision);
        Assert.Equal(23f / 488, placed.UvMin.Y, Precision);
        Assert.Equal(506f / 508, placed.UvMax.X, Precision);
        Assert.Equal(1, placed.UvMax.Y, Precision);
    }

    [Fact]
    public void Place_ClearPixelsAroundThePicture_KeepsOnlyThePartWithColor()
    {
        // Arrange
        byte[] indices =
        [
            9, 0, 0, 0,
            0, 0, 5, 0,
            0, 6, 5, 0,
            0, 0, 0, 0,
        ];
        PlacedSprite sprite = new(new IndexedSprite(4, 4, indices), 0, 0, []);
        SpriteChunk chunk = new(0, 1, 4, 3, -2, -3, 4, 3, 0, 0);

        // Act
        PlacedChunk? placed = ChunkPlacement.Place(chunk, sprite);

        // Assert
        Assert.NotNull(placed);
        Assert.Equal((-1f, -3f, 2f, 2f), (placed.X, placed.Y, placed.Width, placed.Height));
        Assert.Equal(new Vector2(0.25f, 0.25f), placed.UvMin);
        Assert.Equal(new Vector2(0.75f, 0.75f), placed.UvMax);
    }

    [Fact]
    public void Place_PieceWithOnlyClearPixels_IsNull()
    {
        // Arrange
        PlacedSprite sprite = new(new IndexedSprite(2, 2, [0, 0, 0, 0]), 0, 0, []);
        SpriteChunk chunk = new(0, 0, 2, 2, 0, 0, 2, 2, 0, 0);

        // Act
        PlacedChunk? placed = ChunkPlacement.Place(chunk, sprite);

        // Assert
        Assert.Null(placed);
    }

    [Fact]
    public void Place_PieceOutsideTheImage_IsNull()
    {
        // Arrange
        PlacedSprite sprite = Sprite(5, 0, 100, 100);
        SpriteChunk chunk = new(480, 0, 32, 12, -40, -360, 32, 12, 1, 0);

        // Act
        PlacedChunk? placed = ChunkPlacement.Place(chunk, sprite);

        // Assert
        Assert.Null(placed);
    }

    [Fact]
    public void Place_PieceDrawnMirrored_ReturnsAPositiveWidthAndFlipsThePicture()
    {
        // Arrange
        PlacedSprite sprite = Sprite(0, 0, 4, 2);
        SpriteChunk chunk = new(0, 0, 4, 2, 2, -2, -4, 2, 0, 0);

        // Act
        PlacedChunk? placed = ChunkPlacement.Place(chunk, sprite);

        // Assert
        Assert.NotNull(placed);
        Assert.Equal((-2f, -2f, 4f, 2f), (placed.X, placed.Y, placed.Width, placed.Height));
        Assert.Equal(new Vector2(1, 0), placed.UvMin);
        Assert.Equal(new Vector2(0, 1), placed.UvMax);
    }

    [Fact]
    public void Place_PieceDrawnUpsideDown_ReturnsAPositiveHeightAndFlipsThePicture()
    {
        // Arrange
        PlacedSprite sprite = Sprite(0, 0, 4, 2);
        SpriteChunk chunk = new(0, 0, 4, 2, -2, 0, 4, -2, 0, 0);

        // Act
        PlacedChunk? placed = ChunkPlacement.Place(chunk, sprite);

        // Assert
        Assert.NotNull(placed);
        Assert.Equal((-2f, -2f, 4f, 2f), (placed.X, placed.Y, placed.Width, placed.Height));
        Assert.Equal(new Vector2(0, 1), placed.UvMin);
        Assert.Equal(new Vector2(1, 0), placed.UvMax);
    }

    [Fact]
    public void Place_ImageStoredAtTwiceTheSize_ReadsThePieceFromTwiceTheSource()
    {
        // Arrange
        PlacedSprite sprite = Sprite(2, 0, 8, 4);
        SpriteChunk chunk = new(0, 0, 6, 2, -3, -2, 6, 2, 0, 0);

        // Act
        PlacedChunk? placed = ChunkPlacement.Place(chunk, sprite, pixelsPerUnit: 2);

        // Assert
        Assert.NotNull(placed);
        Assert.Equal((-2f, -2f, 4f, 2f), (placed.X, placed.Y, placed.Width, placed.Height));
        Assert.Equal(Vector2.Zero, placed.UvMin);
        Assert.Equal(Vector2.One, placed.UvMax);
    }

    private static PlacedSprite Sprite(int x, int y, int width, int height) =>
        new(new IndexedSprite(width, height, Enumerable.Repeat((byte)1, width * height).ToArray()), x, y, []);
}
