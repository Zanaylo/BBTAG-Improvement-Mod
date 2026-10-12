using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

public static unsafe class ProtectedMemory
{
    public static void Write(nint address, ReadOnlySpan<byte> bytes)
    {
        if (!Kernel32.VirtualProtect(address, (nuint)bytes.Length, Kernel32.PageExecuteReadWrite, out var previous))
            throw new PatchFailedException($"Could not unlock 0x{address:X8} for writing (error {Marshal.GetLastPInvokeError()}).");

        bytes.CopyTo(new Span<byte>((void*)address, bytes.Length));
        Kernel32.VirtualProtect(address, (nuint)bytes.Length, previous, out _);
    }

    public static nint SwapPointer(nint slot, nint replacement)
    {
        var original = *(nint*)slot;
        Span<byte> bytes = stackalloc byte[sizeof(nint)];
        MemoryMarshal.Write(bytes, in replacement);
        Write(slot, bytes);
        return original;
    }
}
