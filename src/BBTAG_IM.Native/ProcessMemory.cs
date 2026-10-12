using System.Runtime.InteropServices;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Native;

public sealed unsafe class ProcessMemory : IGameMemory
{
    private MemoryRegion? _lastRegion;

    public void ForgetRegions() => _lastRegion = null;

    public bool CanRead(uint address, int length) => Covers(address, length, region => region.IsReadable);

    public bool CanWrite(uint address, int length) => Covers(address, length, region => region.IsWritable);

    public void Read(uint address, Span<byte> destination) =>
        new ReadOnlySpan<byte>((void*)(nint)address, destination.Length).CopyTo(destination);

    public void Write(uint address, ReadOnlySpan<byte> source) =>
        source.CopyTo(new Span<byte>((void*)(nint)address, source.Length));

    private bool Covers(uint address, int length, Func<MemoryRegion, bool> allows)
    {
        if (address == 0 || length < 0)
            return false;

        var end = (ulong)address + (ulong)length;
        var cursor = (ulong)address;

        while (cursor < end)
        {
            if (RegionAt(cursor) is not { } region || !allows(region))
                return false;

            cursor = region.End;
        }

        return true;
    }

    private MemoryRegion? RegionAt(ulong address)
    {
        if (_lastRegion is { } cached && cached.Contains(address))
            return cached;

        if (Kernel32.VirtualQuery((nint)address, out var region, (nuint)Marshal.SizeOf<MemoryRegion>()) == 0)
            return null;

        _lastRegion = region;
        return region;
    }
}
