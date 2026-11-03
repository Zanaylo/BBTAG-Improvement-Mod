using System.Buffers.Binary;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Sprites;

public sealed record IndexedSprite(int Width, int Height, byte[] Indices);

public sealed record PlacedSprite(IndexedSprite Image, int X, int Y, IReadOnlyList<PaletteColor> Colors);

public static class HipImage
{
    private const int HeaderLength = ImageHeaderStart + ImageFieldsLength;
    private const int PaletteCountField = 0x0C;
    private const int PictureWidthField = 0x10;
    private const int PictureHeightField = 0x14;
    private const int ImageHeaderLengthField = 0x1C;
    private const int ImageHeaderStart = 0x20;
    private const int WidthField = 0x20;
    private const int HeightField = 0x24;
    private const int XField = 0x28;
    private const int YField = 0x2C;
    private const int ImageFieldsLength = 4 * sizeof(int);
    private const int BytesPerPaletteColor = 4;
    private const int BytesPerRun = 2;
    private const int LargestSide = 0x4000;

    private static readonly byte[] Signature = "HIP\0"u8.ToArray();

    public static PlacedSprite Place(ReadOnlySpan<byte> hip)
    {
        var bounds = Bounds(hip);
        return new PlacedSprite(Decode(hip), bounds.X, bounds.Y, Colors(hip));
    }

    public static IndexedSprite Decode(ReadOnlySpan<byte> hip)
    {
        var bounds = Bounds(hip);
        var start = ImageHeaderStart + Int32(hip, ImageHeaderLengthField) + Int32(hip, PaletteCountField) * BytesPerPaletteColor;

        if (bounds.Width is <= 0 or > LargestSide || bounds.Height is <= 0 or > LargestSide || start < ImageHeaderStart || start > hip.Length)
            throw new InvalidSpriteDataException("Its size is damaged.");

        return new IndexedSprite(bounds.Width, bounds.Height, Expand(hip[start..], bounds.Width * bounds.Height));
    }

    private static SpriteBounds Bounds(ReadOnlySpan<byte> hip)
    {
        if (hip.Length < HeaderLength || !hip.StartsWith(Signature))
            throw new InvalidSpriteDataException("It is not a sprite this mod can read.");

        if (!HasImageHeader(hip))
            return new SpriteBounds(0, 0, Int32(hip, PictureWidthField), Int32(hip, PictureHeightField));

        return new SpriteBounds(Int32(hip, XField), Int32(hip, YField), Int32(hip, WidthField), Int32(hip, HeightField));
    }

    private static bool HasImageHeader(ReadOnlySpan<byte> hip) => Int32(hip, ImageHeaderLengthField) >= ImageFieldsLength;

    private static PaletteColor[] Colors(ReadOnlySpan<byte> hip)
    {
        var start = ImageHeaderStart + Int32(hip, ImageHeaderLengthField);
        var count = Math.Clamp(Int32(hip, PaletteCountField), 0, Math.Max(hip.Length - start, 0) / BytesPerPaletteColor);
        var colors = new PaletteColor[count];

        for (var index = 0; index < count; index++)
            colors[index] = PaletteColors.Decode(hip.Slice(start + index * BytesPerPaletteColor, BytesPerPaletteColor));

        return colors;
    }

    private static int Int32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);

    private static byte[] Expand(ReadOnlySpan<byte> runs, int pixelCount)
    {
        var indices = new byte[pixelCount];
        var filled = 0;

        for (var run = 0; run + 1 < runs.Length && filled < pixelCount; run += BytesPerRun)
        {
            var length = runs[run + 1];

            if (filled + length > pixelCount)
                throw new InvalidSpriteDataException("Its pixels run past the image.");

            indices.AsSpan(filled, length).Fill(runs[run]);
            filled += length;
        }

        if (filled != pixelCount)
            throw new InvalidSpriteDataException("Its pixels do not fill the image. It may use a format this mod cannot read.");

        return indices;
    }

    private readonly record struct SpriteBounds(int X, int Y, int Width, int Height);
}
