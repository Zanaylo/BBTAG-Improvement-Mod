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
    public const uint FreeNodes = 0x04000800;
    public const int FileRoom = 0x1000;
    public const uint NodeVtable = 0x00B7309C;
    public const uint KeyVtable = 0x00AB0E44;
    public const int BucketCount = 2;

    private const uint BucketArrayOffset = 0x100;
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
        uint bucket = Bucket(map, name);
        uint tail = Memory.ReadUInt32(bucket + 8);
        byte[] field = new byte[ScriptBinary.NameLength];
        Encoding.Latin1.GetBytes(name, field);

        Memory.UInt32At(node, NodeVtable)
            .UInt32At(node + 8, tail)
            .UInt32At(node + 0xC, KeyVtable)
            .BytesAt(node + 0x10, field)
            .Int32At(node + 0x30, offset)
            .UInt32At(tail == 0 ? bucket + 4 : tail + 4, node)
            .UInt32At(bucket + 8, node)
            .Int32At(bucket + 0xC, Memory.Int32From(bucket + 0xC) + 1);
        return node;
    }

    public uint Bucket(uint map, string name) => map + BucketArrayOffset + ScriptMapHash.Of(name) % BucketCount * BucketLength;

    public IReadOnlyList<uint> BucketChain(uint map, string name)
    {
        uint bucket = Bucket(map, name);
        List<uint> chain = [];

        for (uint node = Memory.ReadUInt32(bucket + 4); node != 0; node = Memory.ReadUInt32(node + 4))
            chain.Add(node);

        return chain;
    }

    private void EmptyMap(uint map) =>
        Memory.UInt32At(map + 0x10, map + BucketArrayOffset).UInt32At(map + 0x14, BucketCount);
}

internal static class FakeGameMemoryReading
{
    public static uint ReadUInt32(this FakeGameMemory memory, uint address) => (uint)memory.Int32From(address);
}
