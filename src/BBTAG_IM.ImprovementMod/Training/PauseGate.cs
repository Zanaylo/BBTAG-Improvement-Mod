using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Training;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Training;

internal sealed unsafe class PauseGate
{
    private const int Held = 1;
    private const int Running = 0;

    private readonly nint _block;

    private PauseGate(nint block, uint site)
    {
        _block = block;
        Site = site;
    }

    public uint Site { get; }

    public bool Holds
    {
        set => Volatile.Write(ref *(int*)(_block + PauseGateCode.HoldOffset), value ? Held : Running);
    }

    public static PauseGate Install(CodeImage code)
    {
        var site = PauseGateCode.Locate(code);
        var block = Kernel32.VirtualAlloc(0, PauseGateCode.BlockLength, Kernel32.MemoryCommit | Kernel32.MemoryReserve, Kernel32.PageExecuteReadWrite);

        if (block == 0)
            throw new PatchFailedException("No memory could be reserved for the pause.");

        PauseGateCode.Code((uint)block).CopyTo(new Span<byte>((void*)(block + PauseGateCode.CodeOffset), PauseGateCode.BlockLength - PauseGateCode.CodeOffset));
        ProtectedMemory.Write((nint)site, PauseGateCode.Jump(site, (uint)block));
        Kernel32.FlushInstructionCache(Kernel32.GetCurrentProcess(), (nint)site, PauseGateCode.JumpLength);
        return new PauseGate(block, site);
    }

    public int TakeCalls() => Interlocked.Exchange(ref *(int*)(_block + PauseGateCode.CallsOffset), 0);
}
