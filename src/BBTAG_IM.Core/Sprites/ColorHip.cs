using System.Buffers.Binary;

namespace BBTAG_IM.Core.Sprites;

public sealed record ColorImage(int Width, int Height, byte[] Bgra);

public static class ColorHip
{
    private const int HeaderLength = 0x20;
    private const int LengthField = 0x08;
    private const int WidthField = 0x10;
    private const int HeightField = 0x14;
    private const int BytesPerPixel = 4;
    private const int RunLength = BytesPerPixel + 1;
    private const int LongestRun = byte.MaxValue;
    private const int LargestSide = 0x4000;

    private static ReadOnlySpan<byte> Signature => "HIP\0"u8;

    public static ColorImage Decode(ReadOnlySpan<byte> hip)
    {
        if (hip.Length < HeaderLength || !hip.StartsWith(Signature))
            throw new InvalidSpriteDataException("It is not a HIP picture.");

        var width = BinaryPrimitives.ReadInt32LittleEndian(hip[WidthField..]);
        var height = BinaryPrimitives.ReadInt32LittleEndian(hip[HeightField..]);

        if (width <= 0 || height <= 0 || width > LargestSide || height > LargestSide)
            throw new InvalidSpriteDataException("Its picture size is damaged.");

        var pixels = new byte[width * height * BytesPerPixel];
        var filled = 0;

        for (var at = HeaderLength; at + RunLength <= hip.Length && filled < pixels.Length; at += RunLength)
        {
            var color = hip.Slice(at, BytesPerPixel);

            for (var count = hip[at + BytesPerPixel]; count > 0 && filled < pixels.Length; count--, filled += BytesPerPixel)
                color.CopyTo(pixels.AsSpan(filled));
        }

        if (filled < pixels.Length)
            throw new InvalidSpriteDataException("Its picture ends too early.");

        return new ColorImage(width, height, pixels);
    }

    public static byte[] Encode(ReadOnlySpan<byte> template, ColorImage image)
    {
        var hip = new List<byte>(template[..HeaderLength].ToArray());
        var pixels = image.Bgra.AsSpan();
        var total = pixels.Length / BytesPerPixel;

        for (var index = 0; index < total;)
        {
            var color = pixels.Slice(index * BytesPerPixel, BytesPerPixel);
            var run = 1;

            while (index + run < total && run < LongestRun && pixels.Slice((index + run) * BytesPerPixel, BytesPerPixel).SequenceEqual(color))
                run++;

            hip.AddRange(color);
            hip.Add((byte)run);
            index += run;
        }

        var bytes = hip.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(LengthField), bytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(WidthField), image.Width);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(HeightField), image.Height);
        return bytes;
    }
}
