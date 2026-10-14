using System.Text;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Live;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class ScriptMemory
{
    public const uint Record = 0x01000000;
    public const uint File = 0x02000000;
    public const uint StateMap = 0x03000000;
    public const uint SubroutineMap = 0x03100000;
    public const uint Nodes = 0x04000000;
    public const int FileRoom = 0x1000;

    private const uint BucketArrayOffset = 0x100;
    private const int BucketCount = 2;
    private const int BucketLength = 0x10;
    private const int NodeLength = 0x34;

    private uint _nextNode = Nodes;

    public ScriptMemory()
    {
        Memory = new FakeGameMemory()
            .Map(Record, ScriptRecord.Length * 2)
            .Map(File, FileRoom)
            .Map(StateMap, 0x200)
            .Map(SubroutineMap, 0x200)
            .Map(Nodes, 0x1000);
    }

    public FakeGameMemory Memory { get; }

    public ScriptMemory Load(byte[] script)
    {
        int stateCount = BitConverter.ToInt32(script, 0);
        Memory.BytesAt(File, script)
            .UInt32At(Record, File)
            .UInt32At(Record + 4, File + (uint)ScriptBinary.DataStartFor(stateCount))
            .UInt32At(Record + 8, (uint)stateCount)
            .UInt32At(Record + 12, File + 4)
            .UInt32At(Record + 16, StateMap)
            .UInt32At(Record + 20, SubroutineMap);
        EmptyMap(StateMap);
        EmptyMap(SubroutineMap);
        return this;
    }

    public uint AddNode(uint map, string name, int offset)
    {
        uint node = _nextNode;
        _nextNode += NodeLength;
        uint bucket = map + BucketArrayOffset + (uint)(name.Length % BucketCount) * BucketLength;
        byte[] field = new byte[ScriptBinary.NameLength];
        Encoding.Latin1.GetBytes(name, field);

        Memory.UInt32At(node + 4, Memory.ReadUInt32(bucket + 4))
            .BytesAt(node + 0x10, field)
            .Int32At(node + 0x30, offset)
            .UInt32At(bucket + 4, node);
        return node;
    }

    private void EmptyMap(uint map) =>
        Memory.UInt32At(map + 0x10, map + BucketArrayOffset).UInt32At(map + 0x14, BucketCount);
}

internal static class FakeGameMemoryReading
{
    public static uint ReadUInt32(this FakeGameMemory memory, uint address) => (uint)memory.Int32From(address);
}
