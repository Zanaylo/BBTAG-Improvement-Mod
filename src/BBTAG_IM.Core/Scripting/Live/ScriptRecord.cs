using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed record ScriptMapNode(uint Address, string Name, int Offset);

public sealed class ScriptRecord(IGameMemory memory, uint address)
{
    public const int Length = 0x18;
    public const uint ObjectRecordOffset = 0x1400;

    private const uint FileOffset = 0;
    private const uint BytecodeOffset = 4;
    private const uint StateCountOffset = 8;
    private const uint StateTableOffset = 0xC;
    private const uint StateMapOffset = 0x10;
    private const uint SubroutineMapOffset = 0x14;
    private const uint BucketsOffset = 0x10;
    private const uint BucketCountOffset = 0x14;
    private const uint BucketLength = 0x10;
    private const uint BucketHeadOffset = 4;
    private const uint NodeNextOffset = 4;
    private const uint NodeNameOffset = 0x10;
    private const uint NodeValueOffset = 0x30;
    private const int MostNodes = 0x10000;

    public uint Address => address;

    public uint File => memory.ReadUInt32(address + FileOffset);

    public uint Bytecode => memory.ReadUInt32(address + BytecodeOffset);

    public int StateCount => memory.ReadInt32(address + StateCountOffset);

    public uint StateTable => memory.ReadUInt32(address + StateTableOffset);

    public uint StateMap => memory.ReadUInt32(address + StateMapOffset);

    public uint SubroutineMap => memory.ReadUInt32(address + SubroutineMapOffset);

    public IEnumerable<uint> Maps => [StateMap, SubroutineMap];

    public IReadOnlyList<ScriptMapNode> Nodes(uint map)
    {
        var nodes = new List<ScriptMapNode>();
        var buckets = memory.ReadUInt32(map + BucketsOffset);
        var count = memory.ReadUInt32(map + BucketCountOffset);

        for (var bucket = 0u; bucket < count; bucket++)
        {
            var node = memory.ReadUInt32(buckets + bucket * BucketLength + BucketHeadOffset);

            while (node != 0 && nodes.Count < MostNodes)
            {
                nodes.Add(new ScriptMapNode(node, memory.ReadText(node + NodeNameOffset, ScriptBinary.NameLength), memory.ReadInt32(node + NodeValueOffset)));
                node = memory.ReadUInt32(node + NodeNextOffset);
            }
        }

        return nodes;
    }

    public void Point(uint file, int stateCount)
    {
        memory.WriteInt32(address + FileOffset, (int)file);
        memory.WriteInt32(address + BytecodeOffset, (int)(file + (uint)ScriptBinary.DataStartFor(stateCount)));
        memory.WriteInt32(address + StateCountOffset, stateCount);
        memory.WriteInt32(address + StateTableOffset, (int)(file + sizeof(int)));
    }

    public void SetOffset(ScriptMapNode node, int offset) => memory.WriteInt32(node.Address + NodeValueOffset, offset);
}
