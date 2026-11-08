using System.Buffers.Binary;

namespace BBTAG_IM.Core.Palettes;

public static class HplWriter
{
    public const int ColorCount = 256;

    private const int HeaderLength = 0x20;
    private const int FormatField = 4;
    private const int LengthField = 8;
    private const int CountField = 12;
    private const int FlagsField = 0x18;
    private const int FormatVersion = 0x125;
    private const int PaletteFlags = 0x10000001;
    private const int BytesPerColor = 4;
    private const int RedByte = 0;
    private const int GreenByte = 1;
    private const int BlueByte = 2;
    private const int AlphaByte = 3;

    private static readonly byte[] Signature = "HPAL"u8.ToArray();

    public static byte[] Write(ReadOnlySpan<byte> rgbaPalette)
    {
        var length = HeaderLength + ColorCount * BytesPerColor;
        var hpl = new byte[length];
        var span = hpl.AsSpan();
        Signature.CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[FormatField..], FormatVersion);
        BinaryPrimitives.WriteInt32LittleEndian(span[LengthField..], length);
        BinaryPrimitives.WriteInt32LittleEndian(span[CountField..], ColorCount);
        BinaryPrimitives.WriteInt32LittleEndian(span[FlagsField..], PaletteFlags);
        CopyBlueFirst(rgbaPalette, span[HeaderLength..]);
        return hpl;
    }

    public static void CopyBlueFirst(ReadOnlySpan<byte> rgba, Span<byte> blueFirst)
    {
        for (var color = 0; color < ColorCount; color++)
        {
            var source = rgba.Slice(color * BytesPerColor, BytesPerColor);
            var target = blueFirst.Slice(color * BytesPerColor, BytesPerColor);
            target[RedByte] = source[BlueByte];
            target[GreenByte] = source[GreenByte];
            target[BlueByte] = source[RedByte];
            target[AlphaByte] = source[AlphaByte];
        }
    }
}
