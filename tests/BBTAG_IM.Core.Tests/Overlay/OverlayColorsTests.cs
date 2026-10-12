using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Tests.Overlay;

public class OverlayColorsTests
{
    [Fact]
    public void SwapRedAndBlue_MenuColor_ReturnsTheDirect3DColor()
    {
        // Act
        uint color = OverlayColors.SwapRedAndBlue(0x80332211);

        // Assert
        Assert.Equal(0x80112233u, color);
    }

    [Fact]
    public void SwapRedAndBlue_AppliedTwice_GivesTheColorBack()
    {
        // Act
        uint color = OverlayColors.SwapRedAndBlue(OverlayColors.SwapRedAndBlue(0xFF699B5F));

        // Assert
        Assert.Equal(0xFF699B5Fu, color);
    }

    [Fact]
    public void ConvertRgbaToBgra_TwoPixels_SwapsRedAndBlueOfEach()
    {
        // Arrange
        byte[] pixels = [1, 2, 3, 4, 5, 6, 7, 8];

        // Act
        OverlayColors.ConvertRgbaToBgra(pixels);

        // Assert
        Assert.Equal([3, 2, 1, 4, 7, 6, 5, 8], pixels);
    }

    [Fact]
    public void Opaque_TransparentColor_SetsFullAlpha()
    {
        // Act
        uint color = OverlayColors.Opaque(0x00112233);

        // Assert
        Assert.Equal(0xFF112233u, color);
    }
}
