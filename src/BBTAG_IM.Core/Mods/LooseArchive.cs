using System.Buffers.Binary;
using System.Text;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Mods;

public sealed record ArchiveFile(string Name, byte[] Bytes);

public static class LooseArchive
{
    private const int Alignment = 16;
    private const int ShortestNameField = 0x10;
    private const int NameFieldStep = 4;
    private const int FieldsAfterName = 3 * sizeof(int);
    private const int NamedFiles = 1;
    private const int TotalSizeField = 8;
    private const int CountField = 12;
    private const int FlagsField = 16;
    private const int NameLengthField = 20;

    private static readonly byte[] Signature = "FPAC"u8.ToArray();

    public static IEnumerable<ArchiveFile> Read(byte[] archive)
    {
        var dataOffset = FpacArchive.DataOffset(archive);

        foreach (var entry in FpacArchive.Entries(archive.AsSpan(0, dataOffset)))
            yield return new ArchiveFile(entry.Name, archive.AsSpan((int)entry.Offset, entry.Size).ToArray());
    }

    public static byte[] Merge(byte[]? original, IReadOnlyList<ArchiveFile> loose)
    {
        var replacements = loose.ToDictionary(file => file.Name, StringComparer.OrdinalIgnoreCase);
        var files = new List<ArchiveFile>();

        foreach (var file in original is null ? [] : Read(original))
        {
            if (replacements.Remove(file.Name, out var replacement))
                files.Add(file with { Bytes = replacement.Bytes });
            else
                files.Add(file);
        }

        files.AddRange(replacements.Values.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase));
        return Write(files);
    }

    public static byte[] Write(IReadOnlyList<ArchiveFile> files)
    {
        var nameLength = NameLength(files);
        var entryLength = Align(nameLength + FieldsAfterName);
        var dataOffset = Align(FpacArchive.PrefixLength + files.Count * entryLength);
        var offsets = new int[files.Count];
        var dataLength = 0;

        for (var index = 0; index < files.Count; index++)
        {
            offsets[index] = dataLength;
            dataLength = Align(dataLength + files[index].Bytes.Length);
        }

        var archive = new byte[dataOffset + dataLength];
        WriteHeader(archive, dataOffset, files.Count, nameLength);

        for (var index = 0; index < files.Count; index++)
        {
            var entry = archive.AsSpan(FpacArchive.PrefixLength + index * entryLength, entryLength);
            Encoding.Latin1.GetBytes(files[index].Name, entry[..nameLength]);
            BinaryPrimitives.WriteInt32LittleEndian(entry[nameLength..], index);
            BinaryPrimitives.WriteInt32LittleEndian(entry[(nameLength + sizeof(int))..], offsets[index]);
            BinaryPrimitives.WriteInt32LittleEndian(entry[(nameLength + 2 * sizeof(int))..], files[index].Bytes.Length);
            files[index].Bytes.CopyTo(archive, dataOffset + offsets[index]);
        }

        return archive;
    }

    private static void WriteHeader(byte[] archive, int dataOffset, int count, int nameLength)
    {
        Signature.CopyTo(archive, 0);
        BinaryPrimitives.WriteInt32LittleEndian(archive.AsSpan(sizeof(int)), dataOffset);
        BinaryPrimitives.WriteInt32LittleEndian(archive.AsSpan(TotalSizeField), archive.Length);
        BinaryPrimitives.WriteInt32LittleEndian(archive.AsSpan(CountField), count);
        BinaryPrimitives.WriteInt32LittleEndian(archive.AsSpan(FlagsField), NamedFiles);
        BinaryPrimitives.WriteInt32LittleEndian(archive.AsSpan(NameLengthField), nameLength);
    }

    private static int NameLength(IReadOnlyList<ArchiveFile> files)
    {
        var longest = files.Count == 0 ? 0 : files.Max(file => file.Name.Length) + 1;
        return Math.Max(ShortestNameField, (longest + NameFieldStep - 1) / NameFieldStep * NameFieldStep);
    }

    private static int Align(int value) => (value + Alignment - 1) / Alignment * Alignment;
}
