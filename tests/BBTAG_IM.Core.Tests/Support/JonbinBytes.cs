using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class JonbinBytes
{
    public const int TypeCount = 17;

    private const int ImageNameLength = 32;
    private const int TypeCountBias = 3;
    private const int ShortLength = 52;
    private const int LongLength = 308;
    private const int MiddleLength = 80;
    private const int LayerField = 64;

    private readonly List<string> _images = [];
    private readonly List<(int Type, float X, float Y, float Width, float Height)> _boxes = [];
    private readonly List<float[]> _chunks = [];
    private int _shortEntries;
    private int _longEntries;
    private int _middleEntries;
    private byte[] _tail = [];

    public JonbinBytes Image(string name)
    {
        _images.Add(name);
        return this;
    }

    public JonbinBytes Sections(int shortEntries, int longEntries, int middleEntries)
    {
        _shortEntries = shortEntries;
        _longEntries = longEntries;
        _middleEntries = middleEntries;
        return this;
    }

    public JonbinBytes Chunk(float sourceX, float sourceY, float sourceWidth, float sourceHeight, float x, float y, float width, float height, int layer)
    {
        _chunks.Add([sourceX, sourceY, sourceWidth, sourceHeight, x, y, width, height, BitConverter.Int32BitsToSingle(layer)]);
        return this;
    }

    public JonbinBytes Box(int type, float x, float y, float width, float height)
    {
        _boxes.Add((type, x, y, width, height));
        return this;
    }

    public JonbinBytes Tail(params byte[] tail)
    {
        _tail = tail;
        return this;
    }

    public byte[] Build()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("JONB"u8);
        writer.Write((ushort)_images.Count);

        foreach (var image in _images)
        {
            var name = new byte[ImageNameLength];
            Encoding.ASCII.GetBytes(image, name);
            writer.Write(name);
        }

        writer.Write((byte)(TypeCount + TypeCountBias));
        writer.Write((ushort)_shortEntries);
        writer.Write((ushort)(_middleEntries + _chunks.Count));
        writer.Write((ushort)_longEntries);

        for (var type = 0; type < TypeCount; type++)
            writer.Write((ushort)_boxes.Count(box => box.Type == type));

        writer.Write(Filler(_shortEntries * ShortLength, 0x10));
        writer.Write(Filler(_longEntries * LongLength, 0x20));
        writer.Write(Filler(_middleEntries * MiddleLength, 0x30));

        foreach (var chunk in _chunks)
        {
            var entry = new byte[MiddleLength];

            for (var field = 0; field < 8; field++)
                BitConverter.TryWriteBytes(entry.AsSpan(field * sizeof(float)), chunk[field]);

            BitConverter.TryWriteBytes(entry.AsSpan(LayerField), BitConverter.SingleToInt32Bits(chunk[8]));
            writer.Write(entry);
        }

        foreach (var box in _boxes.OrderBy(box => box.Type))
        {
            writer.Write(box.Type);
            writer.Write(box.X);
            writer.Write(box.Y);
            writer.Write(box.Width);
            writer.Write(box.Height);
        }

        writer.Write(_tail);
        return stream.ToArray();
    }

    private static byte[] Filler(int length, byte seed)
    {
        var bytes = new byte[length];

        for (var index = 0; index < length; index++)
            bytes[index] = (byte)(seed + index);

        return bytes;
    }
}
