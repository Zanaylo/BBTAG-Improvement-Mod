using System.Buffers.Binary;

namespace BBTAG_IM.Core.Hantei;

public static class PalFile
{
    public const int PaletteLength = 1024;

    private const int HeaderLength = 16;
    private const int CountField = 12;

    public static IReadOnlyList<byte[]> Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderLength)
            throw new InvalidHanteiDataException("It is not a palette file.");

        var count = BinaryPrimitives.ReadInt32LittleEndian(bytes[CountField..]);

        if (count < 0 || HeaderLength + (long)count * PaletteLength > bytes.Length)
            throw new InvalidHanteiDataException("The palette file ends too early.");

        var palettes = new byte[count][];

        for (var index = 0; index < count; index++)
            palettes[index] = bytes.Slice(HeaderLength + index * PaletteLength, PaletteLength).ToArray();

        return palettes;
    }
}
