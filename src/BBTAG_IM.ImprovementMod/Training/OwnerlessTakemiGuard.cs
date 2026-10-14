using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Training;

internal sealed unsafe class OwnerlessTakemiGuard
{
    private readonly nint _block;
    private readonly ILog _log;
    private bool _reported;

    private OwnerlessTakemiGuard(nint block, uint site, ILog log)
    {
        _block = block;
        _log = log;
        Site = site;
    }

    public uint Site { get; }

    public static OwnerlessTakemiGuard Install(CodeImage code, ILog log)
    {
        var site = OwnerlessTakemiGuardCode.Locate(code);
        var block = Kernel32.VirtualAlloc(0, OwnerlessTakemiGuardCode.BlockLength, Kernel32.MemoryCommit | Kernel32.MemoryReserve, Kernel32.PageExecuteReadWrite);

        if (block == 0)
            throw new PatchFailedException("No memory could be reserved for the Take-Mikazuchi body guard.");

        OwnerlessTakemiGuardCode.Code(site, (uint)block).CopyTo(new Span<byte>((void*)(block + OwnerlessTakemiGuardCode.CodeOffset), OwnerlessTakemiGuardCode.BlockLength - OwnerlessTakemiGuardCode.CodeOffset));
        ProtectedMemory.Write((nint)site, OwnerlessTakemiGuardCode.Jump(site, (uint)block));
        Kernel32.FlushInstructionCache(Kernel32.GetCurrentProcess(), (nint)site, OwnerlessTakemiGuardCode.ReplacedLength);
        return new OwnerlessTakemiGuard(block, site, log);
    }

    public void OnPresent(nint device)
    {
        var skips = Interlocked.Exchange(ref *(int*)(_block + OwnerlessTakemiGuardCode.SkipsOffset), 0);

        if (skips == 0 || _reported)
            return;

        _reported = true;
        SafeLog.Info(_log, "A Take-Mikazuchi body part had no owner. Its collision update was skipped instead of crashing the game.");
    }
}
