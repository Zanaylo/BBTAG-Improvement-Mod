using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed record JonbinBox(int Type, float X, float Y, float Width, float Height)
{
    public float LowX => MathF.Min(X, X + Width);

    public float HighX => MathF.Max(X, X + Width);

    public float LowY => MathF.Min(Y, Y + Height);

    public float HighY => MathF.Max(Y, Y + Height);
}

public sealed record SpriteChunk(float SourceX, float SourceY, float SourceWidth, float SourceHeight, float X, float Y, float Width, float Height, int Layer, int Image);

public sealed record SpritePiece(string Image, SpriteChunk Chunk);

public sealed class Jonbin
{
    public const string Extension = ".jonbin";
    public const int BoxLength = 20;

    private const int ImageCountField = 4;
    private const int FirstImageField = 6;
    private const int ImageNameLength = 32;
    private const int TypeCountBias = 3;
    private const int ShortEntryLength = 52;
    private const int MiddleEntryLength = 80;
    private const int LongEntryLength = 308;
    private const int ShortCountField = 1;
    private const int MiddleCountField = 3;
    private const int LongCountField = 5;
    private const int SectionCountsLength = 7;
    private const int BoxXField = 4;
    private const int BoxYField = 8;
    private const int BoxWidthField = 12;
    private const int BoxHeightField = 16;
    private const int ChunkFields = 8;
    private const int ChunkImageField = 60;
    private const int ChunkLayerField = 64;
    private const string PlaceholderImage = "999_99";

    private static readonly byte[] Signature = "JONB"u8.ToArray();

    private readonly byte[] _head;
    private readonly byte[] _sections;
    private readonly byte[] _tail;
    private readonly Pictures _pictures;

    private Jonbin(byte[] head, byte[] sections, byte[] tail, int typeCount, IReadOnlyList<JonbinBox> boxes, Pictures pictures)
    {
        _head = head;
        _sections = sections;
        _tail = tail;
        TypeCount = typeCount;
        Boxes = boxes;
        _pictures = pictures;
    }

    public IReadOnlyList<string> Images => _pictures.Images;

    public IReadOnlyList<SpriteChunk> Chunks => _pictures.Chunks;

    public IReadOnlyList<SpritePiece> Pieces => _pictures.Pieces;

    public int TypeCount { get; }

    public IReadOnlyList<JonbinBox> Boxes { get; }

    public int Length => _head.Length + TypeCount * sizeof(ushort) + _sections.Length + Boxes.Count * BoxLength + _tail.Length;

    public static Jonbin Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < FirstImageField || !bytes.StartsWith(Signature))
            throw new InvalidCollisionDataException("It is not a collision file.");

        var counts = FirstImageField + BinaryPrimitives.ReadUInt16LittleEndian(bytes[ImageCountField..]) * ImageNameLength;
        Require(bytes, counts + SectionCountsLength);

        var typeCount = bytes[counts] - TypeCountBias;
        var sectionsStart = counts + SectionCountsLength + typeCount * sizeof(ushort);
        Require(bytes, sectionsStart);

        var boxesStart = sectionsStart + SectionsLength(bytes[counts..]);
        Require(bytes, boxesStart);
        var boxes = ReadBoxes(bytes, counts + SectionCountsLength, typeCount, boxesStart);
        var tailStart = boxesStart + boxes.Count * BoxLength;
        var pictures = new Pictures(ReadImages(bytes, counts), ReadChunks(bytes[sectionsStart..boxesStart], bytes[counts..]));

        return new Jonbin(
            bytes[..(counts + SectionCountsLength)].ToArray(),
            bytes[sectionsStart..boxesStart].ToArray(),
            bytes[tailStart..].ToArray(),
            typeCount,
            boxes,
            pictures);
    }

    public Jonbin With(IEnumerable<JonbinBox> boxes)
    {
        var ordered = boxes.OrderBy(box => box.Type).ToList();

        if (ordered.FirstOrDefault(box => box.Type < 0 || box.Type >= TypeCount) is { } unknown)
            throw new InvalidCollisionDataException($"Collision files have no box type {unknown.Type}.");

        return new Jonbin(_head, _sections, _tail, TypeCount, ordered, _pictures);
    }

    public byte[] Write()
    {
        var bytes = new byte[Length];
        var span = bytes.AsSpan();
        _head.CopyTo(span);
        var cursor = _head.Length;

        for (var type = 0; type < TypeCount; type++, cursor += sizeof(ushort))
            BinaryPrimitives.WriteUInt16LittleEndian(span[cursor..], (ushort)Boxes.Count(box => box.Type == type));

        _sections.CopyTo(span[cursor..]);
        cursor += _sections.Length;

        foreach (var box in Boxes)
        {
            WriteBox(span.Slice(cursor, BoxLength), box);
            cursor += BoxLength;
        }

        _tail.CopyTo(span[cursor..]);
        return bytes;
    }

    private static int SectionsLength(ReadOnlySpan<byte> counts) =>
        BinaryPrimitives.ReadUInt16LittleEndian(counts[ShortCountField..]) * ShortEntryLength
        + BinaryPrimitives.ReadUInt16LittleEndian(counts[MiddleCountField..]) * MiddleEntryLength
        + BinaryPrimitives.ReadUInt16LittleEndian(counts[LongCountField..]) * LongEntryLength;

    private static string[] ReadImages(ReadOnlySpan<byte> bytes, int end)
    {
        var count = (end - FirstImageField) / ImageNameLength;
        var images = new string[count];

        for (var index = 0; index < count; index++)
        {
            var name = bytes.Slice(FirstImageField + index * ImageNameLength, ImageNameLength);
            var terminator = name.IndexOf((byte)0);
            images[index] = Encoding.Latin1.GetString(terminator < 0 ? name : name[..terminator]);
        }

        return images;
    }

    private static SpriteChunk[] ReadChunks(ReadOnlySpan<byte> sections, ReadOnlySpan<byte> counts)
    {
        var start = BinaryPrimitives.ReadUInt16LittleEndian(counts[ShortCountField..]) * ShortEntryLength
            + BinaryPrimitives.ReadUInt16LittleEndian(counts[LongCountField..]) * LongEntryLength;
        var chunks = new SpriteChunk[BinaryPrimitives.ReadUInt16LittleEndian(counts[MiddleCountField..])];

        for (var index = 0; index < chunks.Length; index++)
            chunks[index] = ReadChunk(sections.Slice(start + index * MiddleEntryLength, MiddleEntryLength));

        return chunks;
    }

    private static SpriteChunk ReadChunk(ReadOnlySpan<byte> entry)
    {
        Span<float> fields = stackalloc float[ChunkFields];

        for (var field = 0; field < ChunkFields; field++)
            fields[field] = BinaryPrimitives.ReadSingleLittleEndian(entry[(field * sizeof(float))..]);

        return new SpriteChunk(
            fields[0],
            fields[1],
            fields[2],
            fields[3],
            fields[4],
            fields[5],
            fields[6],
            fields[7],
            BinaryPrimitives.ReadInt32LittleEndian(entry[ChunkLayerField..]),
            BinaryPrimitives.ReadInt32LittleEndian(entry[ChunkImageField..]));
    }

    private static List<JonbinBox> ReadBoxes(ReadOnlySpan<byte> bytes, int countsStart, int typeCount, int boxesStart)
    {
        List<JonbinBox> boxes = [];
        var cursor = boxesStart;

        for (var type = 0; type < typeCount; type++)
        {
            var count = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(countsStart + type * sizeof(ushort))..]);
            Require(bytes, cursor + count * BoxLength);

            for (var index = 0; index < count; index++, cursor += BoxLength)
                boxes.Add(ReadBox(bytes.Slice(cursor, BoxLength), type));
        }

        return boxes;
    }

    private static JonbinBox ReadBox(ReadOnlySpan<byte> box, int type) =>
        new(
            type,
            BinaryPrimitives.ReadSingleLittleEndian(box[BoxXField..]),
            BinaryPrimitives.ReadSingleLittleEndian(box[BoxYField..]),
            BinaryPrimitives.ReadSingleLittleEndian(box[BoxWidthField..]),
            BinaryPrimitives.ReadSingleLittleEndian(box[BoxHeightField..]));

    private static void WriteBox(Span<byte> destination, JonbinBox box)
    {
        BinaryPrimitives.WriteInt32LittleEndian(destination, box.Type);
        BinaryPrimitives.WriteSingleLittleEndian(destination[BoxXField..], box.X);
        BinaryPrimitives.WriteSingleLittleEndian(destination[BoxYField..], box.Y);
        BinaryPrimitives.WriteSingleLittleEndian(destination[BoxWidthField..], box.Width);
        BinaryPrimitives.WriteSingleLittleEndian(destination[BoxHeightField..], box.Height);
    }

    private static void Require(ReadOnlySpan<byte> bytes, int length)
    {
        if (bytes.Length < length)
            throw new InvalidCollisionDataException("The collision file ends too early.");
    }

    private static bool IsPlaceholder(string image) =>
        Path.GetFileNameWithoutExtension(image).EndsWith(PlaceholderImage, StringComparison.Ordinal);

    private sealed record Pictures(IReadOnlyList<string> Images, IReadOnlyList<SpriteChunk> Chunks)
    {
        public IReadOnlyList<SpritePiece> Pieces { get; } = Chunks
            .Where(chunk => chunk.Image >= 0 && chunk.Image < Images.Count && !IsPlaceholder(Images[chunk.Image]))
            .OrderBy(chunk => chunk.Layer)
            .Select(chunk => new SpritePiece(Images[chunk.Image], chunk))
            .ToArray();
    }
}
