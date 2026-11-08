using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Porting;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Porting;

public class SheetPicturesTests
{
    private static readonly byte[] Palette = new byte[1024];
    private static readonly CgImage Image = new("000_001.bmp", 864, 640, 10, 20, 2, 1, [5, 6]);

    [Fact]
    public void Single_Image_KeepsItsPlaceOnTheCanvas()
    {
        // Act
        byte[] hip = SheetPictures.Single(Image, Palette);

        // Assert
        PlacedSprite placed = HipImage.Place(hip);
        Assert.Equal((10, 20, 2, 1), (placed.X, placed.Y, placed.Image.Width, placed.Image.Height));
        Assert.Equal(1024, BitConverter.ToInt32(hip, 0x10));
    }

    [Fact]
    public void Double_Image_DoublesThePlaceThePixelsAndThePicture()
    {
        // Act
        byte[] hip = SheetPictures.Double(Image, Palette);

        // Assert
        PlacedSprite placed = HipImage.Place(hip);
        Assert.Equal((20, 40, 4, 2), (placed.X, placed.Y, placed.Image.Width, placed.Image.Height));
        Assert.Equal([5, 5, 6, 6, 5, 5, 6, 6], placed.Image.Indices);
        Assert.Equal(2048, BitConverter.ToInt32(hip, 0x10));
    }

    [Fact]
    public void Single_WideCanvas_SizesEachSideSeparately()
    {
        // Arrange
        CgImage wide = Image with { CanvasWidth = 1280, CanvasHeight = 640 };

        // Act
        byte[] hip = SheetPictures.Single(wide, Palette);

        // Assert
        Assert.Equal(2048, BitConverter.ToInt32(hip, 0x10));
        Assert.Equal(1024, BitConverter.ToInt32(hip, 0x14));
    }

    [Theory]
    [InlineData("000_001.bmp", "Kh", "Kh000_001")]
    [InlineData("990_03.bmp", "Hyd", "Hyd990_03")]
    public void Name_Image_PrefixesTheStem(string image, string prefix, string expected)
    {
        // Act
        string name = SheetPictures.Name(image, prefix);

        // Assert
        Assert.Equal(expected, name);
    }
}
