using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Hantei;

public class CgFileTests
{
    [Fact]
    public void Read_NotCg_Throws()
    {
        // Arrange
        byte[] bytes = new byte[0x5000];

        // Act
        Exception? exception = Record.Exception(() => CgFile.Read(bytes));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }

    [Fact]
    public void Image_OneTile_PlacesItsPixelsInsideTheBounds()
    {
        // Arrange
        byte[] tile = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        byte[] bytes = new CgBytes().Image("000_000.bmp", 64, 16, 32, 31, 47, [tile], [(16, 32)]).Build();

        // Act
        CgImage? image = CgFile.Read(bytes).Image(0);

        // Assert
        Assert.NotNull(image);
        Assert.Equal("000_000.bmp", image.Name);
        Assert.Equal((64, 64, 16, 32, 16, 16), (image.CanvasWidth, image.CanvasHeight, image.Left, image.Top, image.Width, image.Height));
        Assert.Equal(tile, image.Indices);
    }

    [Fact]
    public void Image_TwoTilesInARow_JoinsThem()
    {
        // Arrange
        byte[] first = Enumerable.Repeat((byte)1, 256).ToArray();
        byte[] second = Enumerable.Repeat((byte)2, 256).ToArray();
        byte[] bytes = new CgBytes().Image("a.bmp", 64, 0, 0, 31, 15, [first, second], [(0, 0), (16, 0)]).Build();

        // Act
        CgImage? image = CgFile.Read(bytes).Image(0);

        // Assert
        Assert.NotNull(image);
        Assert.Equal(1, image.Indices[0]);
        Assert.Equal(2, image.Indices[16]);
        Assert.Equal(2, image.Indices[32 * 16 - 1]);
    }

    [Fact]
    public void Image_PastTheLastImage_IsNull()
    {
        // Arrange
        byte[] bytes = new CgBytes().Image("a.bmp", 32, 0, 0, 15, 15, [new byte[256]], [(0, 0)]).Build();

        // Act
        CgImage? image = CgFile.Read(bytes).Image(1);

        // Assert
        Assert.Null(image);
    }

    [Fact]
    public void Image_NotIndexed_IsNull()
    {
        // Arrange
        byte[] bytes = new CgBytes().Image("a.bmp", 32, 0, 0, 15, 15, [new byte[256]], [(0, 0)], bpp: 32).Build();

        // Act
        CgImage? image = CgFile.Read(bytes).Image(0);

        // Assert
        Assert.Null(image);
    }
}
