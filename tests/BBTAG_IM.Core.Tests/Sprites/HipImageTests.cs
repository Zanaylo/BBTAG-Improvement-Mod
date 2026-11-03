using BBTAG_IM.Core.Palettes;
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
    public void Place_CroppedImage_ReturnsWhereItSitsOnTheFullPicture()
    {
        // Arrange
        byte[] hip = SpriteFiles.HipAt(32, 210, 3, 2, (0, 2), (7, 3), (9, 1));

        // Act
        PlacedSprite sprite = HipImage.Place(hip);

        // Assert
        Assert.Equal((32, 210), (sprite.X, sprite.Y));
        Assert.Equal([0, 0, 7, 7, 7, 9], sprite.Image.Indices);
    }

    [Fact]
    public void Place_ImageWithItsOwnColors_ReadsThem()
    {
        // Arrange
        byte[] hip = SpriteFiles.HipAt(0, 0, 1, 1, (1, 1));
        hip[SpriteFiles.HipPaletteStart + 4] = 0x30;
        hip[SpriteFiles.HipPaletteStart + 5] = 0x20;
        hip[SpriteFiles.HipPaletteStart + 6] = 0x10;
        hip[SpriteFiles.HipPaletteStart + 7] = 0xFF;

        // Act
        PlacedSprite sprite = HipImage.Place(hip);

        // Assert
        Assert.Equal(256, sprite.Colors.Count);
        Assert.Equal(new PaletteColor(0x10, 0x20, 0x30, 0xFF), sprite.Colors[1]);
    }

    [Fact]
    public void Place_FileWithoutAnImageHeader_FillsTheWholePictureFromTheCorner()
    {
        // Arrange
        byte[] hip = SpriteFiles.HeaderlessHip(3, 2, (0, 2), (7, 3), (9, 1));

        // Act
        PlacedSprite sprite = HipImage.Place(hip);

        // Assert
        Assert.Equal((0, 0, 3, 2), (sprite.X, sprite.Y, sprite.Image.Width, sprite.Image.Height));
        Assert.Equal([0, 0, 7, 7, 7, 9], sprite.Image.Indices);
    }

    [Fact]
    public void Place_FileWithoutAnImageHeader_ReadsTheColorsRightAfterTheFileHeader()
    {
        // Arrange
        byte[] hip = SpriteFiles.HeaderlessHip(1, 1, (1, 1));
        hip[SpriteFiles.HeaderlessHipPaletteStart + 4] = 0x30;
        hip[SpriteFiles.HeaderlessHipPaletteStart + 5] = 0x20;
        hip[SpriteFiles.HeaderlessHipPaletteStart + 6] = 0x10;
        hip[SpriteFiles.HeaderlessHipPaletteStart + 7] = 0xFF;

        // Act
        PlacedSprite sprite = HipImage.Place(hip);

        // Assert
        Assert.Equal(new PaletteColor(0x10, 0x20, 0x30, 0xFF), sprite.Colors[1]);
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
