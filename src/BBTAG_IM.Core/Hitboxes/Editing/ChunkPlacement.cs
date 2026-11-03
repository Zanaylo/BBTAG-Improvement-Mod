using System.Numerics;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed record PlacedChunk(float X, float Y, float Width, float Height, Vector2 UvMin, Vector2 UvMax);

public static class ChunkPlacement
{
    public const float SamePixels = 1;

    private const byte Clear = 0;

    public static PlacedChunk? Place(SpriteChunk chunk, PlacedSprite sprite, float pixelsPerUnit = SamePixels)
    {
        if (chunk.SourceWidth <= 0 || chunk.SourceHeight <= 0)
            return null;

        var source = new Source(chunk.SourceX * pixelsPerUnit, chunk.SourceY * pixelsPerUnit, chunk.SourceWidth * pixelsPerUnit, chunk.SourceHeight * pixelsPerUnit);
        var image = sprite.Image;
        var area = new PixelArea(
            Math.Max((int)MathF.Floor(source.X) - sprite.X, 0),
            Math.Max((int)MathF.Floor(source.Y) - sprite.Y, 0),
            Math.Min((int)MathF.Ceiling(source.X + source.Width) - sprite.X, image.Width),
            Math.Min((int)MathF.Ceiling(source.Y + source.Height) - sprite.Y, image.Height));

        if (Visible(image, area) is not { } visible)
            return null;

        var scaleX = chunk.Width / source.Width;
        var scaleY = chunk.Height / source.Height;
        var (x, width, uLow, uHigh) = Upright(
            chunk.X + (sprite.X + visible.Left - source.X) * scaleX,
            (visible.Right - visible.Left) * scaleX,
            (float)visible.Left / image.Width,
            (float)visible.Right / image.Width);
        var (y, height, vLow, vHigh) = Upright(
            chunk.Y + (sprite.Y + visible.Top - source.Y) * scaleY,
            (visible.Bottom - visible.Top) * scaleY,
            (float)visible.Top / image.Height,
            (float)visible.Bottom / image.Height);

        return new PlacedChunk(x, y, width, height, new Vector2(uLow, vLow), new Vector2(uHigh, vHigh));
    }

    private static (float Start, float Length, float UvAtStart, float UvAtEnd) Upright(float start, float length, float uvLow, float uvHigh) =>
        length < 0 ? (start + length, -length, uvHigh, uvLow) : (start, length, uvLow, uvHigh);

    private static PixelArea? Visible(IndexedSprite image, PixelArea area)
    {
        int left = area.Right, top = area.Bottom, right = area.Left, bottom = area.Top;

        for (var y = area.Top; y < area.Bottom; y++)
        {
            for (var x = area.Left; x < area.Right; x++)
            {
                if (image.Indices[y * image.Width + x] == Clear)
                    continue;

                left = Math.Min(left, x);
                right = Math.Max(right, x + 1);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y + 1);
            }
        }

        return right > left && bottom > top ? new PixelArea(left, top, right, bottom) : null;
    }

    private readonly record struct Source(float X, float Y, float Width, float Height);

    private readonly record struct PixelArea(int Left, int Top, int Right, int Bottom);
}
