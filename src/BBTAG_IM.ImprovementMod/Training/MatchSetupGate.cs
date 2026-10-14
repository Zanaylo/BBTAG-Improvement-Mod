using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Training;

internal sealed unsafe class MatchSetupGate
{
    private readonly nint _block;

    private MatchSetupGate(nint block, uint site)
    {
        _block = block;
        Site = site;
    }

    public uint Site { get; }

    public static MatchSetupGate Install(CodeImage code)
    {
        var site = MatchSetupCode.Locate(code);
        var called = MatchSetupCode.CalledBy(code, site);
        var block = Kernel32.VirtualAlloc(0, MatchSetupCode.BlockLength, Kernel32.MemoryCommit | Kernel32.MemoryReserve, Kernel32.PageExecuteReadWrite);

        if (block == 0)
            throw new PatchFailedException("No memory could be reserved for the match setup.");

        var gate = new MatchSetupGate(block, site);
        gate.Keep();

        MatchSetupCode.Code(site, (uint)block, called).CopyTo(new Span<byte>((void*)(block + MatchSetupCode.CodeOffset), MatchSetupCode.BlockLength - MatchSetupCode.CodeOffset));
        ProtectedMemory.Write((nint)site, MatchSetupCode.Jump(site, (uint)block));
        Kernel32.FlushInstructionCache(Kernel32.GetCurrentProcess(), (nint)site, MatchSetupCode.JumpLength);
        return gate;
    }

    public void Apply(MatchSetup setup)
    {
        for (var slot = 0; slot < MatchSlots.Count; slot++)
        {
            Volatile.Write(ref Field(MatchSetupCode.CharactersOffset + slot * sizeof(int)), setup.Slots[slot].Character);
            Volatile.Write(ref Field(MatchSetupCode.ColorsOffset + slot * sizeof(int)), setup.Slots[slot].Color);
            Volatile.Write(ref Field(MatchSetupCode.PresencesOffset + slot * sizeof(int)), (int)setup.Slots[slot].Presence);
        }

        Volatile.Write(ref Field(MatchSetupCode.StageOffset), setup.Stage);
    }

    public void Keep() => Apply(MatchSetup.Keep);

    public int TakeSetups() => Interlocked.Exchange(ref Field(MatchSetupCode.CountOffset), 0);

    private ref int Field(int offset) => ref *(int*)(_block + offset);
}
