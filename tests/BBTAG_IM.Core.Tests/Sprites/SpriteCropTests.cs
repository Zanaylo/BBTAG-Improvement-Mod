using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Sprites;

public class SpriteCropTests
{
    [Fact]
    public void MainBody_BodyWithAStrayDotInACorner_CropsToTheBodyOnly()
    {
        // Arrange
        IndexedSprite sprite = Sprite(
            "1.........",
            "..........",
            "...22222..",
            "...22222..",
            "...22222..",
            "...22222..",
            ".......3..");

        // Act
        IndexedSprite cropped = SpriteCrop.MainBody(sprite);

        // Assert
        Assert.Equal((5, 5), (cropped.Width, cropped.Height));
        Assert.Equal([0, 0, 0, 0, 3], cropped.Indices[^5..]);
    }

    [Fact]
    public void MainBody_TwoLargeParts_KeepsBoth()
    {
        // Arrange
        IndexedSprite sprite = Sprite(
            "22......",
            "22...44.",
            ".....44.");

        // Act
        IndexedSprite cropped = SpriteCrop.MainBody(sprite);

        // Assert
        Assert.Equal((7, 3), (cropped.Width, cropped.Height));
    }

    [Fact]
    public void MainBody_EmptySprite_ReturnsItUnchanged()
    {
        // Arrange
        IndexedSprite sprite = Sprite("...", "...");

        // Act
        IndexedSprite cropped = SpriteCrop.MainBody(sprite);

        // Assert
        Assert.Same(sprite, cropped);
    }

    private static IndexedSprite Sprite(params string[] rows)
    {
        byte[] indices = rows.SelectMany(row => row.Select(pixel => pixel == '.' ? (byte)0 : (byte)(pixel - '0'))).ToArray();
        return new IndexedSprite(rows[0].Length, rows.Length, indices);
    }
}
