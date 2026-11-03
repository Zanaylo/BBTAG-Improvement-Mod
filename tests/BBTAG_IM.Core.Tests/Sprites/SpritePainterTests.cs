using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Sprites;

public class SpritePainterTests
{
    private const int Aegis = 24;
    private const byte Teal = 5;
    private const byte Slate = 6;

    private static readonly ImplPalette Palette = CreatePalette();

    [Fact]
    public void Paint_SameSize_CopiesThePaletteColorsAndLeavesIndexZeroClear()
    {
        // Arrange
        IndexedSprite sprite = new(2, 1, [0, Teal]);
        uint[] pixels = new uint[2];

        // Act
        SpritePainter.Paint(sprite, Palette, 2, 1, pixels);

        // Assert
        Assert.Equal([0x00000000u, 0xFF102030u], pixels);
    }

    [Fact]
    public void Colored_SpriteWithColors_GivesEveryPixelItsOpaqueColorAndLeavesIndexZeroClear()
    {
        // Arrange
        IndexedSprite sprite = new(3, 1, [0, Teal, Slate]);

        // Act
        uint[] pixels = SpritePainter.Colored(sprite, SpritePainter.CharacterColors(Palette.Colors()));

        // Assert
        Assert.Equal([0x00000000u, 0xFF102030u, 0xFF304050u], pixels);
    }

    [Fact]
    public void Paint_HalfSize_AveragesTheColorsEachPixelCovers()
    {
        // Arrange
        IndexedSprite sprite = new(2, 1, [Teal, Slate]);
        uint[] pixels = new uint[1];

        // Act
        SpritePainter.Paint(sprite, Palette, 1, 1, pixels);

        // Assert
        Assert.Equal(0xFF203040u, pixels[0]);
    }

    [Fact]
    public void Paint_HalfSizeOverAClearPixel_KeepsTheColorAtHalfOpacity()
    {
        // Arrange
        IndexedSprite sprite = new(2, 1, [Teal, 0]);
        uint[] pixels = new uint[1];

        // Act
        SpritePainter.Paint(sprite, Palette, 1, 1, pixels);

        // Assert
        Assert.Equal(0x7F102030u, pixels[0]);
    }

    [Fact]
    public void Paint_DestinationSmallerThanTheSize_ThrowsArgumentException()
    {
        // Arrange
        IndexedSprite sprite = new(2, 1, [Teal, Slate]);

        // Act
        Exception? exception = Record.Exception(() => SpritePainter.Paint(sprite, Palette, 2, 1, new uint[1]));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void Fit_TallSprite_KeepsItsShapeInsideTheBox()
    {
        // Arrange
        IndexedSprite sprite = new(100, 200, new byte[100 * 200]);

        // Act
        (int width, int height) = SpritePainter.Fit(sprite, 50, 50);

        // Assert
        Assert.Equal((25, 50), (width, height));
    }

    [Fact]
    public void Fit_VeryThinSprite_KeepsAtLeastOnePixel()
    {
        // Arrange
        IndexedSprite sprite = new(1, 400, new byte[400]);

        // Act
        (int width, int height) = SpritePainter.Fit(sprite, 50, 50);

        // Assert
        Assert.Equal((1, 50), (width, height));
    }

    private static ImplPalette CreatePalette()
    {
        PaletteColors colors = new();
        colors.Set(0, Teal, new PaletteColor(0x10, 0x20, 0x30, 0xFF));
        colors.Set(0, Slate, new PaletteColor(0x30, 0x40, 0x50, 0xFF));
        return ImplPalette.Create("Minato", Aegis, "", "", colors);
    }
}
