using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed class MoveList(IGameMemory memory, uint player)
{
    public const int MostMoves = 100;

    private const uint ListOffset = 0x8FFC;
    private const int RecordLength = 0x2B4;
    private const uint CountOffset = 0x16C14;
    private const uint CurrentOffset = 0x16C18;
    private const uint NamesOffset = 0x16C1C;
    private const uint NameIndexOffset = 0x1900;
    private const uint NameOrderOffset = 0x1C20;
    private const uint NameCountOffset = 0x1F40;
    private const int NoCurrent = -1;

    private uint List => player + ListOffset;

    private uint Names => List + NamesOffset;

    public int Count => memory.ReadInt32(List + CountOffset);

    public string NameAt(int index) => memory.ReadText(RecordAt(index), ScriptBinary.NameLength);

    public int? StartOf(IReadOnlyList<string> names)
    {
        if (names.Count == 0)
            return null;

        var moves = Enumerable.Range(0, Count).Select(NameAt).ToList();

        for (var start = 0; start + names.Count <= moves.Count; start++)
        {
            if (moves.Skip(start).Take(names.Count).SequenceEqual(names))
                return start;
        }

        return null;
    }

    public byte[] Read(int from, int count) => count <= 0 ? [] : memory.ReadBytes(RecordAt(from), count * RecordLength);

    public void Truncate(int count)
    {
        memory.WriteInt32(List + CountOffset, count);
        memory.WriteInt32(List + CurrentOffset, NoCurrent);
        memory.WriteInt32(Names + NameCountOffset, count);
    }

    public void Append(byte[] records)
    {
        var count = Count;
        memory.Write(RecordAt(count), records);
        memory.WriteInt32(List + CountOffset, count + records.Length / RecordLength);
    }

    public void IndexNames()
    {
        var count = Count;
        var names = Enumerable.Range(0, count).Select(index => memory.ReadBytes(RecordAt(index), ScriptBinary.NameLength)).ToList();
        var order = Enumerable.Range(0, count).OrderBy(index => names[index], NameOrder.Instance).ToList();

        for (var index = 0; index < count; index++)
        {
            memory.Write(Names + (uint)(index * ScriptBinary.NameLength), names[index]);
            memory.WriteInt32(Names + NameIndexOffset + (uint)(index * sizeof(int)), index);
            memory.WriteInt32(Names + NameOrderOffset + (uint)(index * sizeof(int)), order[index]);
        }

        memory.WriteInt32(Names + NameCountOffset, count);
        memory.WriteInt32(List + CurrentOffset, NoCurrent);
    }

    private uint RecordAt(int index) => List + (uint)(index * RecordLength);

    private sealed class NameOrder : IComparer<byte[]>
    {
        public static readonly NameOrder Instance = new();

        public int Compare(byte[]? first, byte[]? second) => Terminated(first).SequenceCompareTo(Terminated(second));

        private static ReadOnlySpan<byte> Terminated(byte[]? name)
        {
            var span = name.AsSpan();
            var end = span.IndexOf((byte)0);
            return end < 0 ? span : span[..end];
        }
    }
}
