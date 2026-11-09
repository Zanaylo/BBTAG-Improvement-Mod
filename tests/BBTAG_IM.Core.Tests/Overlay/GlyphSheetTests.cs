using BBTAG_IM.Core.Overlay;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Overlay;

public class GlyphSheetTests
{
    private const int Cell = 10;
    private const int BytesPerPixel = 4;
    private const byte Opaque = 0xFF;

    private static readonly GlyphGrid Grid = new(Cell, Columns: 2, Characters: "AB1");

    [Fact]
    public void Glyph_CharacterOnTheSheet_ReturnsItsCellTrimmedToTheInk()
    {
        // Arrange
        GlyphSheet sheet = GlyphSheet.Measure(SheetWithInk((0, 2, 6), (1, 1, 9), (2, 4, 5)), Grid);

        // Act
        Glyph? glyph = sheet.Glyph('B');

        // Assert
        Assert.Equal(new Glyph(Cell + 1, 0, 8, Cell), glyph);
    }

    [Fact]
    public void Glyph_CharacterMissing_ReturnsNull()
    {
        // Arrange
        GlyphSheet sheet = GlyphSheet.Measure(SheetWithInk((0, 2, 6)), Grid);

        // Act
        Glyph? glyph = sheet.Glyph('Z');

        // Assert
        Assert.Null(glyph);
    }

    [Fact]
    public void Layout_Text_PlacesGlyphsOneAfterAnotherWithSpacing()
    {
        // Arrange
        GlyphSheet sheet = GlyphSheet.Measure(SheetWithInk((0, 2, 6), (1, 1, 9), (2, 4, 5)), Grid);

        // Act
        IReadOnlyList<PlacedGlyph> placed = sheet.Layout("AB", 0.5f);

        // Assert
        Assert.Equal(
            [
                new PlacedGlyph(new Glyph(2, 0, 4, Cell), 0, 0, 2, 5),
                new PlacedGlyph(new Glyph(Cell + 1, 0, 8, Cell), 2 + GlyphSheet.Spacing * 0.5f, 0, 4, 5),
            ],
            placed);
    }

    [Fact]
    public void Layout_SpaceAndUnknownCharacters_AdvanceWithoutGlyphs()
    {
        // Arrange
        GlyphSheet sheet = GlyphSheet.Measure(SheetWithInk((0, 2, 6)), Grid);

        // Act
        IReadOnlyList<PlacedGlyph> placed = sheet.Layout("A ?A", 1f);

        // Assert
        Assert.Equal(2, placed.Count);
        Assert.Equal(4 + GlyphSheet.Spacing + sheet.SpaceWidth + GlyphSheet.Spacing + sheet.SpaceWidth + GlyphSheet.Spacing, placed[1].X);
    }

    [Fact]
    public void Width_Text_IsWhereTheLastGlyphEnds()
    {
        // Arrange
        GlyphSheet sheet = GlyphSheet.Measure(SheetWithInk((0, 2, 6), (1, 1, 9), (2, 4, 5)), Grid);

        // Act
        float width = sheet.Width("AB", 1f);

        // Assert
        Assert.Equal(4 + GlyphSheet.Spacing + 8, width);
    }

    private static ColorImage SheetWithInk(params (int Index, int Left, int Right)[] glyphs)
    {
        const int width = Cell * 2;
        const int height = Cell * 2;
        var pixels = new byte[width * height * BytesPerPixel];

        foreach (var (index, left, right) in glyphs)
        {
            var cellX = index % Grid.Columns * Cell;
            var cellY = index / Grid.Columns * Cell;

            for (var x = left; x < right; x++)
                pixels[((cellY + Cell / 2) * width + cellX + x) * BytesPerPixel + 3] = Opaque;
        }

        return new ColorImage(width, height, pixels);
    }
}
