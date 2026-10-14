using System.Buffers.Binary;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed record GameCommandTable(uint Address, int Count)
{
    private const string Name = "Script command sizes";
    private const int LastIndexOffset = 3;
    private const int TableOffset = 19;
    private const int EntryLength = 2 * sizeof(uint);

    private static readonly BytePattern SizeLookup = BytePattern.Parse("33 C9 BE ?? ?? ?? ?? 90 8D 04 0E 99 2B C2 D1 F8 8B 14 C5 ?? ?? ?? ??");

    public static GameCommandTable Locate(CodeImage image)
    {
        var site = image.FindSingle(SizeLookup, Name);
        var code = image.Slice(site, SizeLookup.Length);

        return new GameCommandTable(
            BinaryPrimitives.ReadUInt32LittleEndian(code[TableOffset..]),
            BinaryPrimitives.ReadInt32LittleEndian(code[LastIndexOffset..]) + 1);
    }

    public IReadOnlyDictionary<int, int> Read(IGameMemory memory)
    {
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
