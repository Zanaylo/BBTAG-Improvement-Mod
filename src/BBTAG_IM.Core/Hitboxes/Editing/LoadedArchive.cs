using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed record ArchiveEntry(string Name, uint Address, int Size, uint OffsetField)
{
    public uint SizeField => OffsetField + sizeof(uint);
}

public sealed class LoadedArchive(IGameMemory memory, uint address)
{
    private const int LargestHeader = 0x100000;

    public uint Address => address;

    public ArchiveEntry? Find(string name)
    {
        if (Header() is not { } header)
            return null;

        var entries = FpacArchive.Entries(header);
        var index = IndexOf(entries, name);

        if (index < 0)
            return null;

        var entry = entries[index];
        return new ArchiveEntry(entry.Name, unchecked(address + (uint)entry.Offset), entry.Size, address + (uint)FpacArchive.OffsetFieldOf(header, index));
    }

    public void Point(ArchiveEntry entry, uint target, int size)
    {
        var dataOffset = (uint)FpacArchive.DataOffset(memory.ReadBytes(address, FpacArchive.PrefixLength));
        memory.WriteInt32(entry.OffsetField, unchecked((int)(target - address - dataOffset)));
        memory.WriteInt32(entry.SizeField, size);
    }

    private byte[]? Header()
    {
        if (!memory.CanRead(address, FpacArchive.PrefixLength))
            return null;

        try
        {
            var dataOffset = FpacArchive.DataOffset(memory.ReadBytes(address, FpacArchive.PrefixLength));
            return dataOffset <= LargestHeader && memory.CanRead(address, dataOffset) ? memory.ReadBytes(address, dataOffset) : null;
        }
        catch (InvalidSpriteDataException)
        {
            return null;
        }
    }

    private static int IndexOf(IReadOnlyList<FpacEntry> entries, string name)
    {
        for (var index = 0; index < entries.Count; index++)
        {
            if (string.Equals(entries[index].Name, name, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return -1;
    }
}
