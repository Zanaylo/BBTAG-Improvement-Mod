using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Sprites;

public sealed record FpacEntry(string Name, long Offset, int Size);

public static class FpacArchive
{
    public const int PrefixLength = 0x20;

    private const int DataOffsetField = 4;
    private const int CountField = 12;
    private const int NameLengthField = 20;
    private const int FieldsAfterName = 3 * sizeof(int);
    private const int OffsetAfterName = sizeof(int);
    private const int SizeAfterName = 2 * sizeof(int);

    private static readonly byte[] Signature = "FPAC"u8.ToArray();

    public static int DataOffset(ReadOnlySpan<byte> prefix)
    {
        if (prefix.Length < PrefixLength || !prefix.StartsWith(Signature))
            throw new InvalidSpriteDataException("It is not a readable archive. Encrypted game data cannot be read.");

        var dataOffset = BinaryPrimitives.ReadInt32LittleEndian(prefix[DataOffsetField..]);

        if (dataOffset < PrefixLength)
            throw new InvalidSpriteDataException("Its list of files is damaged.");

        return dataOffset;
    }

    public static IReadOnlyList<FpacEntry> Entries(ReadOnlySpan<byte> header)
    {
        if (IsEmpty(header))
            return [];

        var layout = Layout(header);
        var entries = new FpacEntry[layout.Count];

        for (var index = 0; index < layout.Count; index++)
            entries[index] = Entry(header.Slice(PrefixLength + index * layout.Stride, layout.Stride), layout.NameLength, layout.DataOffset);

        return entries;
    }

    public static int OffsetFieldOf(ReadOnlySpan<byte> header, int index)
    {
        var layout = Layout(header);
        return PrefixLength + index * layout.Stride + layout.NameLength + OffsetAfterName;
    }

    private static bool IsEmpty(ReadOnlySpan<byte> header) =>
        DataOffset(header) == PrefixLength && BinaryPrimitives.ReadInt32LittleEndian(header[CountField..]) == 0;

    private static EntryLayout Layout(ReadOnlySpan<byte> header)
    {
        var dataOffset = DataOffset(header);
        var count = BinaryPrimitives.ReadInt32LittleEndian(header[CountField..]);
        var nameLength = BinaryPrimitives.ReadInt32LittleEndian(header[NameLengthField..]);

        if (header.Length < dataOffset || count <= 0 || nameLength <= 0)
            throw new InvalidSpriteDataException("Its list of files is damaged.");

        var stride = (dataOffset - PrefixLength) / count;

        if (stride < nameLength + FieldsAfterName)
            throw new InvalidSpriteDataException("Its list of files is damaged.");

        return new EntryLayout(dataOffset, count, nameLength, stride);
    }

    private static FpacEntry Entry(ReadOnlySpan<byte> entry, int nameLength, int dataOffset)
    {
        var name = entry[..nameLength];
        var terminator = name.IndexOf((byte)0);
        var fields = entry[nameLength..];

        return new FpacEntry(
            Encoding.Latin1.GetString(terminator < 0 ? name : name[..terminator]),
            dataOffset + (long)BinaryPrimitives.ReadUInt32LittleEndian(fields[OffsetAfterName..]),
            BinaryPrimitives.ReadInt32LittleEndian(fields[SizeAfterName..]));
    }

    private readonly record struct EntryLayout(int DataOffset, int Count, int NameLength, int Stride);
}
