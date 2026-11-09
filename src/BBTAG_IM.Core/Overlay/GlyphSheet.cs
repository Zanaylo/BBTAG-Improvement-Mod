using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Overlay;

public sealed record GlyphGrid(int CellSize, int Columns, string Characters);

public sealed record Glyph(int X, int Y, int Width, int Height);

public sealed record PlacedGlyph(Glyph Glyph, float X, float Y, float Width, float Height);

public sealed class GlyphSheet : IHudFont
{
    public const float Spacing = 2;

    private const int BytesPerPixel = 4;
    private const int AlphaByte = 3;
    private const byte InkThreshold = 24;
    private const float SpaceShare = 0.3f;

    private readonly Dictionary<char, Glyph> _glyphs;
    private readonly int _cellSize;

    private GlyphSheet(ColorImage image, int cellSize, Dictionary<char, Glyph> glyphs)
    {
        Image = image;
        _cellSize = cellSize;
        _glyphs = glyphs;
    }

    public ColorImage Image { get; }

    public float SpaceWidth => _cellSize * SpaceShare;

    public float CellHeight => _cellSize;

    public static GlyphSheet Measure(ColorImage image, GlyphGrid grid)
    {
        var glyphs = new Dictionary<char, Glyph>();

        for (var index = 0; index < grid.Characters.Length; index++)
        {
            var left = index % grid.Columns * grid.CellSize;
            var top = index / grid.Columns * grid.CellSize;

            if (InkSpan(image, left, top, grid.CellSize) is { } ink)
                glyphs[grid.Characters[index]] = new Glyph(left + ink.First, top, ink.Last - ink.First + 1, grid.CellSize);
        }

        return new GlyphSheet(image, grid.CellSize, glyphs);
    }

    public Glyph? Glyph(char character) => _glyphs.GetValueOrDefault(character);

    public float LineHeight(float scale) => _cellSize * scale;

    public IReadOnlyList<PlacedGlyph> Layout(string text, float scale)
    {
        var placed = new List<PlacedGlyph>(text.Length);
        var x = 0f;

        foreach (var character in text)
        {
            if (Glyph(character) is not { } glyph)
            {
                x += (SpaceWidth + Spacing) * scale;
                continue;
            }

            placed.Add(new PlacedGlyph(glyph, x, 0, glyph.Width * scale, glyph.Height * scale));
            x += (glyph.Width + Spacing) * scale;
        }

        return placed;
    }

    public float Width(string text, float scale) =>
        Layout(text, scale) is [.., var last] ? last.X + last.Width : 0;

    private static (int First, int Last)? InkSpan(ColorImage image, int left, int top, int cellSize)
    {
        var first = int.MaxValue;
        var last = int.MinValue;
        var right = Math.Min(left + cellSize, image.Width);
        var bottom = Math.Min(top + cellSize, image.Height);

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (image.Bgra[(y * image.Width + x) * BytesPerPixel + AlphaByte] < InkThreshold)
                    continue;

                first = Math.Min(first, x - left);
                last = Math.Max(last, x - left);
            }
        }

        return first > last ? null : (first, last);
    }
}
