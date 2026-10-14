using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Training;

internal sealed unsafe class SittingOutSpawnGate
{
    private readonly nint _block;
    private readonly ILog _log;
    private bool _reported;

    private SittingOutSpawnGate(nint block, uint site, ILog log)
    {
        _block = block;
        _log = log;
        Site = site;
    }

    public uint Site { get; }

    public static SittingOutSpawnGate Install(CodeImage code, uint game, ILog log)
    {
        var site = SittingOutSpawnGateCode.Locate(code);
        var block = Kernel32.VirtualAlloc(0, SittingOutSpawnGateCode.BlockLength, Kernel32.MemoryCommit | Kernel32.MemoryReserve, Kernel32.PageExecuteReadWrite);

        if (block == 0)
            throw new PatchFailedException("No memory could be reserved for the sitting out spawn gate.");

        SittingOutSpawnGateCode.Code(site, (uint)block, game).CopyTo(new Span<byte>((void*)(block + SittingOutSpawnGateCode.CodeOffset), SittingOutSpawnGateCode.BlockLength - SittingOutSpawnGateCode.CodeOffset));
        ProtectedMemory.Write((nint)site, SittingOutSpawnGateCode.Jump(site, (uint)block));
        Kernel32.FlushInstructionCache(Kernel32.GetCurrentProcess(), (nint)site, SittingOutSpawnGateCode.ReplacedLength);
        return new SittingOutSpawnGate(block, site, log);
    }

    public void OnPresent(nint device)
    {
        var skips = Interlocked.Exchange(ref *(int*)(_block + SittingOutSpawnGateCode.SkipsOffset), 0);

        if (skips == 0 || _reported)
            return;

        _reported = true;
        SafeLog.Info(_log, $"A character that sits out the match tried to create {skips} object(s). They were skipped, so a solo boss is not built twice.");
    }
}
