using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Sprites;

public class HipImageTests
{
    [Fact]
    public void Decode_RunsOfColorIndices_ExpandsThemRowByRow()
    {
        // Arrange
        byte[] hip = SpriteFiles.Hip(3, 2, (0, 2), (7, 3), (9, 1));

        // Act
        IndexedSprite sprite = HipImage.Decode(hip);

        // Assert
        Assert.Equal((3, 2), (sprite.Width, sprite.Height));
        Assert.Equal([0, 0, 7, 7, 7, 9], sprite.Indices);
    }

    [Fact]
    public void Decode_RunsThatDoNotFillTheImage_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] hip = SpriteFiles.Hip(3, 2, (0, 2), (7, 3));

        // Act
        Exception? exception = Record.Exception(() => HipImage.Decode(hip));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }

    [Fact]
    public void Decode_RunsPastTheImage_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] hip = SpriteFiles.Hip(2, 1, (5, 3));

        // Act
        Exception? exception = Record.Exception(() => HipImage.Decode(hip));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }

    [Fact]
    public void Decode_FileWithoutTheImageSignature_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] hip = SpriteFiles.Hip(1, 1, (5, 1));
        hip[0] = (byte)'X';

        // Act
        Exception? exception = Record.Exception(() => HipImage.Decode(hip));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }
}
