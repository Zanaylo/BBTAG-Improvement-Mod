using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

public static unsafe class MemoryMap
{
    private const uint PrivateMemory = 0x20000;
    private const ulong LowestAddress = 0x10000;
    private const ulong HighestAddress = 0xFFFF0000;

    public static IEnumerable<MemoryRegion> PrivateWritableRegions()
    {
        var cursor = LowestAddress;

        while (cursor < HighestAddress)
        {
            if (Kernel32.VirtualQuery((nint)cursor, out var region, (nuint)Marshal.SizeOf<MemoryRegion>()) == 0 || region.RegionSize == 0)
                yield break;

            if (region.IsWritable && region.Type == PrivateMemory)
                yield return region;

            cursor = region.End;
        }
    }

    public static bool TryRead(uint address, Span<byte> destination)
    {
        fixed (byte* buffer = destination)
        {
            return Kernel32.ReadProcessMemory(Kernel32.GetCurrentProcess(), (nint)address, buffer, (nuint)destination.Length, out var read)
                && read == (nuint)destination.Length;
        }
    }
}
