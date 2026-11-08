using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

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

    public uint Address => address;

    public uint File => memory.ReadUInt32(address + FileOffset);

    public uint Bytecode => memory.ReadUInt32(address + BytecodeOffset);

    public int StateCount => memory.ReadInt32(address + StateCountOffset);

    public uint StateTable => memory.ReadUInt32(address + StateTableOffset);

    public uint StateMap => memory.ReadUInt32(address + StateMapOffset);

    public uint SubroutineMap => memory.ReadUInt32(address + SubroutineMapOffset);

    public IEnumerable<uint> Maps => [StateMap, SubroutineMap];

    public IReadOnlyList<ScriptMapNode> Nodes(uint map) => Map(map).Nodes();

    public ScriptMap Map(uint map) => new(memory, map);

    public void Point(uint file, int stateCount)
    {
        memory.WriteInt32(address + FileOffset, (int)file);
        memory.WriteInt32(address + BytecodeOffset, (int)(file + (uint)ScriptBinary.DataStartFor(stateCount)));
        memory.WriteInt32(address + StateCountOffset, stateCount);
        memory.WriteInt32(address + StateTableOffset, (int)(file + sizeof(int)));
    }

    public void SetOffset(ScriptMapNode node, int offset) => memory.WriteInt32(node.Address + ScriptMap.NodeValueOffset, offset);
}
