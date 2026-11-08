using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Sprites;

public class HipWriterTests
{
    private static readonly byte[] Palette = new byte[1024];

    [Fact]
    public void Write_Sprite_ReadsBackThePixelsAtTheirPlace()
    {
        // Arrange
        byte[] indices = [0, 0, 0, 0, 0, 7, 8, 0, 0, 9, 9, 0, 0, 0, 0, 0];
        IndexedPicture picture = new(4, 4, 10, 20, indices);

        // Act
        byte[] hip = HipWriter.Write(picture, 64, 64, Palette);

        // Assert
        PlacedSprite placed = HipImage.Place(hip);
        Assert.Equal((11, 21, 2, 2), (placed.X, placed.Y, placed.Image.Width, placed.Image.Height));
        Assert.Equal([7, 8, 9, 9], placed.Image.Indices);
    }

    [Fact]
    public void Write_RunLongerThanOneByte_SplitsIt()
    {
        // Arrange
        byte[] indices = Enumerable.Repeat((byte)3, 1000).ToArray();
        IndexedPicture picture = new(500, 2, 0, 0, indices);

        // Act
        byte[] hip = HipWriter.Write(picture, 1024, 1024, Palette);

        // Assert
        Assert.Equal(indices, HipImage.Decode(hip).Indices);
    }

    [Fact]
    public void Write_FullyTransparent_KeepsOnePixel()
    {
        // Arrange
        IndexedPicture picture = new(2, 2, 0, 0, new byte[4]);

        // Act
        byte[] hip = HipWriter.Write(picture, 64, 64, Palette);

        // Assert
        Assert.Equal(1, HipImage.Decode(hip).Width);
        Assert.Equal(1, HipImage.Decode(hip).Height);
    }

    [Fact]
    public void Write_Palette_IsStoredBlueFirst()
    {
        // Arrange
        byte[] palette = new byte[1024];
        palette[0] = 0x11;
        palette[1] = 0x22;
        palette[2] = 0x33;
        palette[3] = 0xFF;
        IndexedPicture picture = new(1, 1, 0, 0, [1]);

        // Act
        byte[] hip = HipWriter.Write(picture, 64, 64, palette);

        // Assert
        Assert.Equal([0x33, 0x22, 0x11, 0xFF], hip.AsSpan(0x40, 4).ToArray());
    }

    [Theory]
    [InlineData(864, 1024)]
    [InlineData(640, 1024)]
    [InlineData(1280, 2048)]
    [InlineData(1024, 1024)]
    [InlineData(200, 256)]
    public void PictureSide_CanvasSide_IsThePowerOfTwoAroundIt(int canvasSide, int expected)
    {
        // Act
        int side = HipWriter.PictureSide(canvasSide);

        // Assert
        Assert.Equal(expected, side);
    }
}
