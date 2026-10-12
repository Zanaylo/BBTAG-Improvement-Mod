using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Native;

public sealed unsafe class ProcessMemory : IGameMemory
{
    private const int RememberedRegions = 16;

    private readonly MemoryRegion[] _regions = new MemoryRegion[RememberedRegions];

    private int _rememberedCount;
    private int _nextSlot;

    public void ForgetRegions()
    {
        _rememberedCount = 0;
        _nextSlot = 0;
    }

    public bool CanRead(uint address, int length) => Covers(address, length, needsWrite: false);

    public bool CanWrite(uint address, int length) => Covers(address, length, needsWrite: true);

    public void Read(uint address, Span<byte> destination) =>
        new ReadOnlySpan<byte>((void*)(nint)address, destination.Length).CopyTo(destination);

    public void Write(uint address, ReadOnlySpan<byte> source) =>
        source.CopyTo(new Span<byte>((void*)(nint)address, source.Length));

    private bool Covers(uint address, int length, bool needsWrite)
    {
        if (address == 0 || length < 0)
            return false;

        var end = (ulong)address + (ulong)length;
        var cursor = (ulong)address;

        while (cursor < end)
        {
            if (!TryFindRegion(cursor, out var region) || !Allows(region, needsWrite))
                return false;

            cursor = region.End;
        }

        return true;
    }

    private static bool Allows(MemoryRegion region, bool needsWrite) => needsWrite ? region.IsWritable : region.IsReadable;

    private bool TryFindRegion(ulong address, out MemoryRegion region)
    {
        for (var index = 0; index < _rememberedCount; index++)
        {
            if (!_regions[index].Contains(address))
                continue;

            region = _regions[index];
            return true;
        }

        if (Kernel32.VirtualQuery((nint)address, out region, (nuint)sizeof(MemoryRegion)) == 0)
            return false;

        Remember(region);
        return true;
    }

    private void Remember(MemoryRegion region)
    {
        _regions[_nextSlot] = region;
        _nextSlot = (_nextSlot + 1) % RememberedRegions;
        _rememberedCount = Math.Min(_rememberedCount + 1, RememberedRegions);
    }
}
