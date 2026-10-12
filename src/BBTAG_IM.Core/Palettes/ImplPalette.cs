using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Palettes;

public sealed class ImplPalette
{
    public const string Extension = ".impl";
    public const int FileCount = 8;
    public const int FileLength = 1024;
    public const int ColorCount = FileLength / sizeof(uint);

    private const int CharacterFile = 0;
    private const int HeaderLength = 20;
    private const int HeaderLengthField = 8;
    private const int DataLengthField = 12;
    private const int CharacterIndexField = 16;
    private const int NameLength = 32;
    private const int CreatorLength = 32;
    private const int DescriptionLength = 64;
    private const int TextLength = NameLength + CreatorLength + DescriptionLength;
    private const int DataLength = TextLength + FileCount * FileLength;

    private static readonly byte[] Signature = "IMPL\0"u8.ToArray();
    private static readonly byte[] FullSignature = "IMPL\0\0\0\0"u8.ToArray();

    private readonly byte[] _files;

    private ImplPalette(string name, int characterIndex, string creator, string description, byte[] files)
    {
        Name = name;
        CharacterIndex = characterIndex;
        Creator = creator;
        Description = description;
        _files = files;
    }

    public string Name { get; }

    public int CharacterIndex { get; }

    public string Creator { get; }

    public string Description { get; }

    public static ImplPalette Parse(string name, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderLength + DataLength)
            throw new InvalidPaletteException($"{name}{Extension} is too short to be a palette.");

        if (!bytes.StartsWith(Signature))
            throw new InvalidPaletteException($"{name}{Extension} is not a palette file.");

        if (BinaryPrimitives.ReadInt32LittleEndian(bytes[DataLengthField..]) != DataLength)
            throw new InvalidPaletteException($"{name}{Extension} holds palette data of an unknown size.");

        var data = bytes.Slice(HeaderLength, DataLength);

        return new ImplPalette(
            name,
            BinaryPrimitives.ReadInt16LittleEndian(bytes[CharacterIndexField..]),
            Text(data.Slice(NameLength, CreatorLength)),
            Text(data.Slice(NameLength + CreatorLength, DescriptionLength)),
            data[TextLength..].ToArray());
    }

    public static ImplPalette Create(string name, int characterIndex, string creator, string description, PaletteColors colors) =>
        new(name, characterIndex, creator, description, colors.Bytes.ToArray());

    public byte[] ToBytes()
    {
        var bytes = new byte[HeaderLength + DataLength];
        FullSignature.CopyTo(bytes, 0);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(HeaderLengthField), HeaderLength);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(DataLengthField), DataLength);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(CharacterIndexField), (short)CharacterIndex);

        var data = bytes.AsSpan(HeaderLength);
        WriteText(data[..NameLength], Name);
        WriteText(data.Slice(NameLength, CreatorLength), Creator);
        WriteText(data.Slice(NameLength + CreatorLength, DescriptionLength), Description);
        _files.CopyTo(data[TextLength..]);
        return bytes;
    }

    public PaletteColors Colors() => PaletteColors.From(_files);

    public ReadOnlySpan<byte> File(int index) => _files.AsSpan(index * FileLength, FileLength);

    public PaletteColor ColorAt(int file, int entry) =>
        PaletteColors.Decode(File(file).Slice(entry * PaletteColors.BytesPerColor, PaletteColors.BytesPerColor));

    public bool IsFileEmpty(int index) => !File(index).ContainsAnyExcept((byte)0);

    public IReadOnlyList<uint> CharacterColors()
    {
        var file = File(CharacterFile);
        var colors = new uint[ColorCount];

        for (var index = 0; index < ColorCount; index++)
            colors[index] = BinaryPrimitives.ReadUInt32LittleEndian(file[(index * sizeof(uint))..]);

        return colors;
    }

    private static void WriteText(Span<byte> field, string text)
    {
        var bytes = Encoding.Latin1.GetBytes(text);
        bytes.AsSpan(0, Math.Min(bytes.Length, field.Length - 1)).CopyTo(field);
    }

    private static string Text(ReadOnlySpan<byte> field)
    {
        var terminator = field.IndexOf((byte)0);
        return Encoding.Latin1.GetString(terminator < 0 ? field : field[..terminator]).Trim();
    }
}
