using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Hantei;

public static class Ha6File
{
    private const int FirstChunk = 0x28;
    private const int StartField = 0x20;
    private const int TagLength = 4;
    private const int LayerIndexField = 0;
    private const int LayerSheetField = 1;
    private const int LayerSpriteField = 2;
    private const int LongDurationField = 0;

    private static readonly byte[] Signature = "Hantei6DataFile"u8.ToArray();
    private static readonly byte[] StartTag = "_STR"u8.ToArray();

    public static IReadOnlyDictionary<int, Ha6Pattern> Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < FirstChunk || !bytes.StartsWith(Signature) || !bytes[StartField..].StartsWith(StartTag))
            throw new InvalidHanteiDataException("It is not a Hantei data file.");

        var cursor = new Cursor(bytes, FirstChunk);
        Dictionary<int, Ha6Pattern> patterns = [];

        while (cursor.CanRead)
        {
            var tag = cursor.Tag();

            if (tag == "_END")
                break;

            if (tag != "PSTR")
                throw Unknown(tag);

            var id = cursor.Int32();

            if (cursor.Peek() == "PEND")
            {
                cursor.Tag();
                continue;
            }

            patterns[id] = ReadPattern(ref cursor, id);
        }

        return patterns;
    }

    public static IReadOnlyDictionary<int, Ha6Pattern> Merge(
        IEnumerable<IReadOnlyDictionary<int, Ha6Pattern>> characterFiles,
        IReadOnlyDictionary<int, Ha6Pattern> sharedTemplate)
    {
        Dictionary<int, Ha6Pattern> merged = [];

        foreach (var file in characterFiles)
        {
            foreach (var (id, pattern) in file.Where(entry => entry.Value.Frames.Count > 0))
                merged[id] = pattern;
        }

        foreach (var (id, pattern) in sharedTemplate.Where(entry => entry.Value.Frames.Count > 0))
        {
            if (!merged.TryGetValue(id, out var character))
                merged[id] = pattern;
            else if (character.Name.Length == 0)
                merged[id] = character with { Name = pattern.Name };
        }

        return merged;
    }

    private static Ha6Pattern ReadPattern(ref Cursor cursor, int id)
    {
        PatternBuilder pattern = new();

        while (cursor.CanRead)
        {
            var tag = cursor.Tag();

            switch (tag)
            {
                case "PEND":
                    return pattern.Build(id);
                case "PTT2":
                    pattern.Name = cursor.Text();
                    break;
                case "PTCN":
                    pattern.Code = cursor.Text();
                    break;
                case "PDS2":
                    cursor.Skip(cursor.Int32() / sizeof(int));
                    break;
                case "FSTR":
                    ReadFrame(ref cursor, pattern);
                    break;
                default:
                    SkipChunk(ref cursor, tag);
                    break;
            }
        }

        throw Truncated();
    }

    private static void ReadFrame(ref Cursor cursor, PatternBuilder pattern)
    {
        var frame = pattern.StartFrame();

        while (cursor.CanRead)
        {
            var tag = cursor.Tag();

            switch (tag)
            {
                case "FEND":
                    return;
                case "HRNM" or "HRAT":
                    pattern.AddBox(frame, cursor.Int32(), tag == "HRAT", ReadBox(ref cursor));
                    break;
                case "HRNS" or "HRAS":
                    var location = cursor.Int32();
                    pattern.AddAlias(frame, location, tag == "HRAS", cursor.Int32());
                    break;
                case "ASSM":
                    frame.StateCopy = cursor.Int32();
                    break;
                case "AFST":
                    ReadAnimation(ref cursor, frame);
                    break;
                case "ASST":
                    ReadBlock(ref cursor, "ASED", frame.State);
                    frame.HasState = true;
                    break;
                case "ATST":
                    ReadBlock(ref cursor, "ATED", frame.Attack);
                    break;
                case "IFST":
                    cursor.Skip(1);
                    ReadBlock(ref cursor, "IFED", []);
                    break;
                case "EFST":
                    cursor.Skip(1);
                    frame.Effects.Add(ReadEffect(ref cursor));
                    break;
                default:
                    SkipChunk(ref cursor, tag);
                    break;
            }
        }

        throw Truncated();
    }

    private static Ha6Box ReadBox(ref Cursor cursor)
    {
        var x1 = cursor.Int32();
        var y1 = cursor.Int32();
        var x2 = cursor.Int32();
        return new Ha6Box(x1, y1, x2, cursor.Int32());
    }

    private static void ReadAnimation(ref Cursor cursor, FrameBuilder frame)
    {
        while (cursor.CanRead)
        {
            var tag = cursor.Tag();

            if (tag == "AFED")
                return;

            if (Ha6Chunks.IsLiteralDuration(tag))
            {
                frame.Duration = tag[^1] - '0';
                continue;
            }

            var values = ReadChunk(ref cursor, tag);

            if (tag == "AFGX")
                frame.Layers.Add(new Ha6Layer(values[LayerIndexField], values[LayerSheetField] != 0, values[LayerSpriteField], 0, 0));
            else if (tag == "AFOF" && frame.Layers.Count > 0)
                frame.Layers[^1] = frame.Layers[^1] with { X = values[0], Y = values[1] };
            else if (tag == "AFDL")
                frame.Duration = values[LongDurationField];

            frame.Animation[tag] = values;
        }

        throw Truncated();
    }

    private static Ha6Effect ReadEffect(ref Cursor cursor)
    {
        Dictionary<string, int[]> chunks = [];
        ReadBlock(ref cursor, "EFED", chunks);
        var parameters = chunks.GetValueOrDefault("EFPR", []);

        return new Ha6Effect(
            chunks.GetValueOrDefault("EFTP", [-1])[0],
            chunks.GetValueOrDefault("EFNO", [-1])[0],
            parameters.Length == 0 ? [] : parameters[1..]);
    }

    private static void ReadBlock(ref Cursor cursor, string terminator, Dictionary<string, int[]> chunks)
    {
        while (cursor.CanRead)
        {
            var tag = cursor.Tag();

            if (tag == terminator)
                return;

            chunks[tag] = ReadChunk(ref cursor, tag);
        }

        throw Truncated();
    }

    private static int[] ReadChunk(ref Cursor cursor, string tag)
    {
        var length = Ha6Chunks.IsCounted(tag) ? cursor.PeekInt32() + 1 : Ha6Chunks.FixedLength(tag) ?? throw Unknown(tag);
        var values = new int[length];

        for (var index = 0; index < length; index++)
            values[index] = cursor.Int32();

        return values;
    }

    private static void SkipChunk(ref Cursor cursor, string tag) => ReadChunk(ref cursor, tag);

    private static InvalidHanteiDataException Unknown(string tag) => new($"Chunk {tag} is not known.");

    private static InvalidHanteiDataException Truncated() => new("The file ends too early.");

    private ref struct Cursor(ReadOnlySpan<byte> bytes, int position)
    {
        private readonly ReadOnlySpan<byte> _bytes = bytes;
        private int _position = position;

        public readonly bool CanRead => _position + TagLength <= _bytes.Length;

        public readonly string Peek() => Encoding.Latin1.GetString(_bytes.Slice(_position, TagLength));

        public string Tag()
        {
            var tag = Peek();
            _position += TagLength;
            return tag;
        }

        public readonly int PeekInt32()
        {
            if (_position + sizeof(int) > _bytes.Length)
                throw Truncated();

            return BinaryPrimitives.ReadInt32LittleEndian(_bytes[_position..]);
        }

        public int Int32()
        {
            var value = PeekInt32();
            _position += sizeof(int);
            return value;
        }

        public void Skip(int dwords)
        {
            _position += dwords * sizeof(int);

            if (_position > _bytes.Length)
                throw Truncated();
        }

        public string Text()
        {
            var length = Int32();

            if (length < 0 || _position + length > _bytes.Length)
                throw Truncated();

            var field = _bytes.Slice(_position, length);
            var terminator = field.IndexOf((byte)0);
            _position += length;
            return Encoding.Latin1.GetString(terminator < 0 ? field : field[..terminator]);
        }
    }

    private sealed class FrameBuilder
    {
        public int Duration { get; set; } = 1;

        public int? StateCopy { get; set; }

        public bool HasState { get; set; }

        public List<Ha6Layer> Layers { get; } = [];

        public Dictionary<string, int[]> Animation { get; } = [];

        public Dictionary<string, int[]> State { get; set; } = [];

        public Dictionary<string, int[]> Attack { get; } = [];

        public List<Ha6Effect> Effects { get; } = [];

        public Dictionary<int, Ha6Box> Boxes { get; } = [];

        public Dictionary<int, Ha6Box> AttackBoxes { get; } = [];

        public Ha6Frame Build() => new()
        {
            Duration = Duration,
            Layers = Layers,
            Animation = Animation,
            State = State,
            Attack = Attack,
            Effects = Effects,
            Boxes = Boxes,
            AttackBoxes = AttackBoxes,
        };
    }

    private sealed class PatternBuilder
    {
        private readonly List<FrameBuilder> _frames = [];
        private readonly List<Ha6Box> _emitted = [];
        private readonly List<(FrameBuilder Frame, int Location, bool Attack, int Source)> _aliases = [];

        public string Name { get; set; } = "";

        public string Code { get; set; } = "";

        public FrameBuilder StartFrame()
        {
            FrameBuilder frame = new();
            _frames.Add(frame);
            return frame;
        }

        public void AddBox(FrameBuilder frame, int location, bool attack, Ha6Box box)
        {
            _emitted.Add(box);

            if (box.X1 != box.X2 && box.Y1 != box.Y2)
                Place(frame, location, attack, box);
        }

        public void AddAlias(FrameBuilder frame, int location, bool attack, int source)
        {
            _aliases.Add((frame, location, attack, source));
            _emitted.Add(new Ha6Box(0, 0, 0, 0));
        }

        public Ha6Pattern Build(int id)
        {
            foreach (var alias in _aliases)
                ResolveAlias(alias.Frame, alias.Location, alias.Attack, alias.Source);

            ResolveStateCopies();
            return new Ha6Pattern(id, Name, _frames.Select(frame => frame.Build()).ToArray(), Code);
        }

        private static void Place(FrameBuilder frame, int location, bool attack, Ha6Box box) =>
            (attack ? frame.AttackBoxes : frame.Boxes)[location] = box;

        private void ResolveAlias(FrameBuilder frame, int location, bool attack, int source)
        {
            if (source < _emitted.Count && _emitted[source] is { } box && box.X1 != box.X2 && box.Y1 != box.Y2)
                Place(frame, location, attack, box);
        }

        private void ResolveStateCopies()
        {
            List<FrameBuilder> stated = [];

            foreach (var frame in _frames)
            {
                if (frame.StateCopy is { } copy)
                {
                    if (copy >= 0 && copy < stated.Count)
                        frame.State = stated[copy].State;

                    continue;
                }

                stated.Add(frame);
            }
        }
    }
}
