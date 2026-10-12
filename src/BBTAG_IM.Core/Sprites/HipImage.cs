using System.Buffers.Binary;

namespace BBTAG_IM.Core.Sprites;

public sealed record IndexedSprite(int Width, int Height, byte[] Indices);

public static class HipImage
{
    private const int PaletteCountField = 0x0C;
    private const int ImageHeaderLengthField = 0x1C;
    private const int ImageHeaderStart = 0x20;
    private const int WidthField = 0x20;
    private const int HeightField = 0x24;
    private const int BytesPerPaletteColor = 4;
    private const int BytesPerRun = 2;
    private const int LargestSide = 0x4000;

    private static readonly byte[] Signature = "HIP\0"u8.ToArray();

    public static IndexedSprite Decode(ReadOnlySpan<byte> hip)
    {
        if (hip.Length < ImageHeaderStart + 2 * sizeof(int) || !hip.StartsWith(Signature))
            throw new InvalidSpriteDataException("It is not a sprite this mod can read.");

        var width = BinaryPrimitives.ReadInt32LittleEndian(hip[WidthField..]);
        var height = BinaryPrimitives.ReadInt32LittleEndian(hip[HeightField..]);
        var start = ImageHeaderStart
            + BinaryPrimitives.ReadInt32LittleEndian(hip[ImageHeaderLengthField..])
            + BinaryPrimitives.ReadInt32LittleEndian(hip[PaletteCountField..]) * BytesPerPaletteColor;

        if (width is <= 0 or > LargestSide || height is <= 0 or > LargestSide || start < ImageHeaderStart || start > hip.Length)
            throw new InvalidSpriteDataException("Its size is damaged.");

        return new IndexedSprite(width, height, Expand(hip[start..], width * height));
    }

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
}
