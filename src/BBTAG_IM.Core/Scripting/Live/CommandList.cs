using System.Buffers.Binary;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed record CommandList(uint Address, int Count)
{
    private const int EntryLength = 2 * sizeof(uint);

    public IReadOnlyDictionary<int, int> ReadSizes(IGameMemory memory)
    {
        if (Count <= 0 || !memory.CanRead(Address, Count * EntryLength))
            throw new InvalidScriptException($"The game's list of {Count} script commands at 0x{Address:X8} cannot be read.");

        var entries = memory.ReadBytes(Address, Count * EntryLength);
        var sizes = new Dictionary<int, int>();

        for (var index = 0; index < Count; index++)
        {
            var id = BinaryPrimitives.ReadUInt32LittleEndian(entries.AsSpan(index * EntryLength));
            var size = BinaryPrimitives.ReadInt32LittleEndian(entries.AsSpan(index * EntryLength + sizeof(uint)));

            if (id <= int.MaxValue)
                sizes[(int)id] = size;
        }

        return sizes;
    }
}
