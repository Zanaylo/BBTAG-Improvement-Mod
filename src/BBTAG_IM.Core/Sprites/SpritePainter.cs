using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Sprites;

public static class SpritePainter
{
    private const int CharacterFile = 0;
    private const byte Clear = 0;
    private const int FullAlpha = 0xFF;
    private const int AlphaShift = 24;
    private const int RedShift = 16;
    private const int GreenShift = 8;

    public static (int Width, int Height) Fit(IndexedSprite sprite, int maxWidth, int maxHeight)
    {
        var scale = Math.Min((double)maxWidth / sprite.Width, (double)maxHeight / sprite.Height);
        return (Math.Max(1, (int)Math.Round(sprite.Width * scale)), Math.Max(1, (int)Math.Round(sprite.Height * scale)));
    }

    public static PaletteColor[] CharacterColors(PaletteColors colors) =>
        [.. Enumerable.Range(0, PaletteColors.ColorsPerFile).Select(entry => colors.Get(CharacterFile, entry))];

    public static uint[] Colored(IndexedSprite sprite, IReadOnlyList<PaletteColor> palette)
    {
        var pixels = new uint[sprite.Indices.Length];

        for (var pixel = 0; pixel < pixels.Length; pixel++)
        {
            var index = sprite.Indices[pixel];

            if (index != Clear && index < palette.Count)
                pixels[pixel] = Opaque(palette[index]);
        }

        return pixels;
    }

    public static void Paint(IndexedSprite sprite, ImplPalette palette, int width, int height, Span<uint> pixels)
    {
        if (width <= 0 || height <= 0 || pixels.Length < width * height)
            throw new ArgumentException($"{width}x{height} pixels do not fit in {pixels.Length}.", nameof(pixels));

        for (var y = 0; y < height; y++)
        {
            var top = y * sprite.Height / height;
            var bottom = Math.Max(top + 1, (y + 1) * sprite.Height / height);

            for (var x = 0; x < width; x++)
            {
                var left = x * sprite.Width / width;
                var right = Math.Max(left + 1, (x + 1) * sprite.Width / width);
                pixels[y * width + x] = Average(sprite, palette, left, top, right, bottom);
            }
        }
    }

    private static uint Average(IndexedSprite sprite, ImplPalette palette, int left, int top, int right, int bottom)
    {
        int red = 0, green = 0, blue = 0, opaque = 0;

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var index = sprite.Indices[y * sprite.Width + x];

                if (index == Clear)
                    continue;

                var color = palette.ColorAt(CharacterFile, index);
                red += color.R;
                green += color.G;
                blue += color.B;
                opaque++;
            }
        }

        if (opaque == 0)
            return 0;

        var alpha = FullAlpha * opaque / ((right - left) * (bottom - top));
        return (uint)(alpha << AlphaShift | red / opaque << RedShift | green / opaque << GreenShift | blue / opaque);
    }

    private static uint Opaque(PaletteColor color) => (uint)(FullAlpha << AlphaShift | color.R << RedShift | color.G << GreenShift | color.B);
}
