using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Tests.Overlay;

public class MenuDrawCommandsTests
{
    private const ulong FontTexture = 0x0E3F9780;
    private const ulong SpriteTexture = 0x0F001000;
    private const int IndexCount = 300;

    private readonly MenuTextureIds _known = new();

    public MenuDrawCommandsTests() => _known.Add(FontTexture);

    [Fact]
    public void CanDraw_CommandOnTheFontTextureInsideTheIndices_ReturnsTrue()
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(FontTexture, _known, 60, 240, IndexCount);

        // Assert
        Assert.True(canDraw);
    }

    [Fact]
    public void CanDraw_UnknownTexture_ReturnsFalse()
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(0x1A140215, _known, 0, 3, IndexCount);

        // Assert
        Assert.False(canDraw);
    }

    [Fact]
    public void CanDraw_IndicesRunPastTheBuffer_ReturnsFalse()
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(FontTexture, _known, 270, 60, IndexCount);

        // Assert
        Assert.False(canDraw);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(4u)]
    public void CanDraw_ElementCountNotWholeTriangles_ReturnsFalse(uint elementCount)
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(FontTexture, _known, 0, elementCount, IndexCount);

        // Assert
        Assert.False(canDraw);
    }

    [Fact]
    public void CanDraw_OffsetSoLargeItWouldWrapAround_ReturnsFalse()
    {
        // Act
        bool canDraw = MenuDrawCommands.CanDraw(FontTexture, _known, uint.MaxValue, 3, IndexCount);

        // Assert
        Assert.False(canDraw);
    }

    [Fact]
    public void CanDraw_TextureAddedForASprite_ReturnsTrue()
    {
        // Arrange
        _known.Add(SpriteTexture);

        // Act
        bool canDraw = MenuDrawCommands.CanDraw(SpriteTexture, _known, 0, 6, IndexCount);

        // Assert
        Assert.True(canDraw);
    }

    [Fact]
    public void CanDraw_TextureRemovedAfterRelease_ReturnsFalse()
    {
        // Arrange
        _known.Add(SpriteTexture);
        _known.Remove(SpriteTexture);

        // Act
        bool canDraw = MenuDrawCommands.CanDraw(SpriteTexture, _known, 0, 6, IndexCount);

        // Assert
        Assert.False(canDraw);
    }

    [Fact]
    public void VertexRange_IndicesOfOneQuad_SpansFromTheLowestToTheHighestVertex()
    {
        // Arrange
        ushort[] indices = [42, 43, 40, 42, 40, 41];

        // Act
        MenuVertexRange range = MenuDrawCommands.VertexRange(indices);

        // Assert
        Assert.Equal(new MenuVertexRange(40, 4), range);
    }

    [Fact]
    public void VertexRange_NoIndices_IsEmpty()
    {
        // Act
        MenuVertexRange range = MenuDrawCommands.VertexRange([]);

        // Assert
        Assert.Equal(new MenuVertexRange(0, 0), range);
    }

    [Theory]
    [InlineData(44, true)]
    [InlineData(43, false)]
    public void FitsIn_VertexCount_IsTrueOnlyWhenTheLastVertexExists(int vertexCount, bool expected)
    {
        // Arrange
        MenuVertexRange range = new(40, 4);

        // Act
        bool fits = range.FitsIn(vertexCount);

        // Assert
        Assert.Equal(expected, fits);
    }
}
