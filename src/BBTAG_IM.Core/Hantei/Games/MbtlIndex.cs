using System.Buffers.Binary;
using System.Text;
using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Core.Hantei.Games;

public sealed record MbtlIndexEntry(string Name, uint Offset, int Size);

public sealed record MbtlIndex(byte[] Key, IReadOnlyList<MbtlIndexEntry> Entries)
{
    public const string AnchorName = "data/chr000/chr000.ha6";
    public const string Unsupported = "This version of MELTY BLOOD TYPE LUMINA is not supported: its file list could not be read.";

    private const int TableStep = sizeof(int);
    private const int NameProbeLength = 80;
    private const int LargestSizeTablePadding = 12;
    private const int LargestOffsetTableGap = 0x1000;
    private const int SampledEntries = 256;
    private const byte FirstPrintable = 0x20;
    private const byte LastPrintable = 0x7E;

    public static IEnumerable<MbtlIndex> Candidates(byte[] executable)
    {
        var image = new FileImage(executable);
        var pointer = FindPointerToAnchor(image);
        var namesStart = TableEdge(image, pointer, -TableStep);
        var namesEnd = TableEdge(image, pointer, TableStep) + TableStep;
        var count = (namesEnd - namesStart) / TableStep;
        var keyStart = namesStart - MbtlCipher.KeyLength;

        if (keyStart < 0)
            throw new InvalidHanteiDataException(Unsupported);

        var key = executable.AsSpan(keyStart, MbtlCipher.KeyLength).ToArray();

        foreach (var (sizes, offsets) in ChainedTables(executable, keyStart, namesEnd, count))
            yield return new MbtlIndex(key, ReadEntries(image, namesStart, sizes, offsets, count));
    }

    private static MbtlIndexEntry[] ReadEntries(FileImage image, int names, int sizes, int offsets, int count)
    {
        var entries = new MbtlIndexEntry[count];

        for (var index = 0; index < count; index++)
            entries[index] = new MbtlIndexEntry(NameAt(image, names + index * TableStep), UInt32At(image.Bytes, offsets + index * TableStep), (int)UInt32At(image.Bytes, sizes + index * TableStep));

        return entries;
    }

    private static int FindPointerToAnchor(FileImage image)
    {
        var text = image.Bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes(AnchorName + "\0"));

        if (text < 0 || image.RvaOf(text) is not { } rva)
            throw new InvalidHanteiDataException(Unsupported);

        var address = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(address, image.Headers.ImageBase + rva);
        var pointer = image.Bytes.AsSpan().IndexOf(address);

        return pointer >= 0 ? pointer : throw new InvalidHanteiDataException(Unsupported);
    }

    private static int TableEdge(FileImage image, int start, int step)
    {
        var edge = start;

        while (IsNamePointer(image, edge + step))
            edge += step;

        return edge;
    }

    private static bool IsNamePointer(FileImage image, int offset)
    {
        if (offset < 0 || offset + sizeof(uint) > image.Bytes.Length)
            return false;

        var address = UInt32At(image.Bytes, offset);

        if (address < image.Headers.ImageBase || image.FileOffsetOf(address - image.Headers.ImageBase) is not { } text)
            return false;

        var probe = image.Bytes.AsSpan(text, Math.Min(NameProbeLength, image.Bytes.Length - text));
        return probe[0] is >= FirstPrintable and <= LastPrintable && probe.Contains((byte)'/');
    }

    private static string NameAt(FileImage image, int offset) => image.ReadAsciiString(UInt32At(image.Bytes, offset) - image.Headers.ImageBase);

    private static IEnumerable<(int Sizes, int Offsets)> ChainedTables(byte[] executable, int keyStart, int namesEnd, int count)
    {
        var sampled = Math.Min(count - 1, SampledEntries);

        for (var padding = 0; padding <= LargestSizeTablePadding; padding += TableStep)
        {
            var sizes = keyStart - count * TableStep - padding;

            for (var gap = 0; sizes >= 0 && gap <= LargestOffsetTableGap; gap += TableStep)
            {
                if (ChainedEntries(executable, sizes, namesEnd + gap, sampled) * 2 >= sampled)
                    yield return (sizes, namesEnd + gap);
            }
        }
    }

    private static int ChainedEntries(byte[] executable, int sizes, int offsets, int sampled)
    {
        if (offsets + (sampled + 1) * TableStep > executable.Length)
            return 0;

        var chained = 0;

        for (var index = 0; index < sampled; index++)
        {
            var offset = UInt32At(executable, offsets + index * TableStep);
            var next = UInt32At(executable, offsets + (index + 1) * TableStep);
            var size = UInt32At(executable, sizes + index * TableStep);

            if (size != 0 && offset + size == next)
                chained++;
        }

        return chained;
    }

    private static uint UInt32At(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
}
