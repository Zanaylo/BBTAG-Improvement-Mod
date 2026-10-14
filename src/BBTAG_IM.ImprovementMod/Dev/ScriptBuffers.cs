using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Dev;

internal sealed unsafe class ScriptBuffers : IScriptBuffers
{
    public uint Allocate(byte[] bytes)
    {
        var block = Kernel32.VirtualAlloc(0, (nuint)bytes.Length, Kernel32.MemoryCommit | Kernel32.MemoryReserve, Kernel32.PageReadWrite);

        if (block == 0)
            throw new PatchFailedException($"No memory could be reserved for a {bytes.Length} byte script.");

        bytes.CopyTo(new Span<byte>((void*)block, bytes.Length));
        return (uint)block;
    }

    public void Release(uint address) => Kernel32.VirtualFree((nint)address, 0, Kernel32.MemoryRelease);
}
