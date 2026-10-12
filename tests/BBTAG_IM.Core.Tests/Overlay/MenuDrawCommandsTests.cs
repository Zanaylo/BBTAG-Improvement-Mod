using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Tests.Overlay;

public class MenuDrawCommandsTests
{
    private const ulong FontTexture = 0x0E3F9780;
    private const int IndexCount = 300;

    [Fact]
    public void CanDraw_CommandOnTheFontTextureInsideTheIndices_ReturnsTrue()
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(FontTexture, FontTexture, 60, 240, IndexCount);

        // Assert
        Assert.True(canDraw);
    }

    [Fact]
    public void CanDraw_UnknownTexture_ReturnsFalse()
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(0x1A140215, FontTexture, 0, 3, IndexCount);

        // Assert
        Assert.False(canDraw);
    }

    [Fact]
    public void CanDraw_IndicesRunPastTheBuffer_ReturnsFalse()
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(FontTexture, FontTexture, 270, 60, IndexCount);

        // Assert
        Assert.False(canDraw);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(4u)]
    public void CanDraw_ElementCountNotWholeTriangles_ReturnsFalse(uint elementCount)
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(FontTexture, FontTexture, 0, elementCount, IndexCount);

        // Assert
        Assert.False(canDraw);
    }

    [Fact]
    public void CanDraw_OffsetSoLargeItWouldWrapAround_ReturnsFalse()
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(FontTexture, FontTexture, uint.MaxValue, 3, IndexCount);

        // Assert
        Assert.False(canDraw);
    }
}
