using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

[StructLayout(LayoutKind.Sequential)]
public struct MemoryRegion
{
    private const uint Committed = 0x1000;
    private const uint Guard = 0x100;
    private const uint ReadableProtections = 0x02 | 0x04 | 0x08 | 0x20 | 0x40 | 0x80;
    private const uint WritableProtections = 0x04 | 0x08 | 0x40 | 0x80;

    public nint BaseAddress;
    public nint AllocationBase;
    public uint AllocationProtect;
    public nuint RegionSize;
    public uint State;
    public uint Protect;
    public uint Type;

    public readonly ulong End => (ulong)BaseAddress + RegionSize;

    public readonly bool IsReadable => IsUsable && (Protect & ReadableProtections) != 0;

    public readonly bool IsWritable => IsUsable && (Protect & WritableProtections) != 0;

    private readonly bool IsUsable => State == Committed && (Protect & Guard) == 0;

    public readonly bool Contains(ulong address) => address >= (ulong)BaseAddress && address < End;
}
