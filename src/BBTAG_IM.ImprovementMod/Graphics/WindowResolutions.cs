using BBTAG_IM.Core.Display;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Graphics;

internal sealed unsafe class WindowResolutions
{
    private WindowResolutions(WindowResolutionSites sites, bool alreadyExtended)
    {
        Sites = sites;
        AlreadyExtended = alreadyExtended;
    }

    public WindowResolutionSites Sites { get; }

    public bool AlreadyExtended { get; }

    public static WindowResolutions Install(LoadedModule game, CodeImage code)
    {
        var sites = WindowResolutionCode.Locate(code);

        if (WindowResolutionCode.Count(code, sites) >= WindowResolutionCode.ExtendedCount)
        {
            SavedResolutionHook.Install(game, new SavedResolutionMap(WindowResolutionCode.Resolutions, WindowResolutionCode.OverhaulSizes(code, sites)));
            Patch(sites.SavedLimit, WindowResolutionCode.SavedLimitPatch());
            return new WindowResolutions(sites, alreadyExtended: true);
        }

        var block = Kernel32.VirtualAlloc(0, WindowResolutionCode.BlockLength, Kernel32.MemoryCommit | Kernel32.MemoryReserve, Kernel32.PageExecuteReadWrite);

        if (block == 0)
            throw new PatchFailedException("No memory could be reserved for the window resolutions.");

        Fill(block, WindowResolutionCode.ListOffset, WindowResolutionCode.List());
        Fill(block, WindowResolutionCode.BorderlessCodeOffset, WindowResolutionCode.BorderlessCode(code, sites, (uint)block));
        Fill(block, WindowResolutionCode.WindowCodeOffset, WindowResolutionCode.WindowCode(code, sites, (uint)block));
        Patch(sites.Count, WindowResolutionCode.CountPatch());
        Patch(sites.Borderless, WindowResolutionCode.BorderlessJump(sites, (uint)block));
        Patch(sites.Window, WindowResolutionCode.WindowJump(sites, (uint)block));
        Patch(sites.SavedLimit, WindowResolutionCode.SavedLimitPatch());
        return new WindowResolutions(sites, alreadyExtended: false);
    }

    private static void Fill(nint block, int offset, byte[] bytes) =>
        bytes.CopyTo(new Span<byte>((void*)(block + offset), bytes.Length));

    private static void Patch(uint site, byte[] bytes)
    {
        ProtectedMemory.Write((nint)site, bytes);
        Kernel32.FlushInstructionCache(Kernel32.GetCurrentProcess(), (nint)site, (nuint)bytes.Length);
    }
}
