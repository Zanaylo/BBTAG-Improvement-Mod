using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Patching;
using BBTAG_IM.Native;

namespace BBTAG_IM.Payload.Patching;

internal static unsafe class MemoryPatcher
{
    private const uint AllocationGranularity = 0x10000;

    public static void Apply(ExePatchPlan plan, LoadedModule game, ILog log)
    {
        var imageBase = (uint)game.BaseAddress;
        var newSectionsAddress = plan.Sections.Count == 0
            ? imageBase + plan.NewSectionRvas.Start
            : ReserveNewSections(imageBase + plan.NewSectionRvas.Start, plan.NewSectionRvas.Length);
        var relocation = new PlanRelocation(plan, imageBase, newSectionsAddress);

        foreach (var section in plan.Sections)
        {
            var bytes = relocation.BytesFor(section);
            bytes.CopyTo(new Span<byte>((void*)relocation.AddressOf(section), bytes.Length));
            log.Info($"Section {section.Name}: 0x{section.VirtualSize:X} bytes at 0x{relocation.AddressOf(section):X8}, {section.AddressFields.Count} address(es) and {section.BranchFields.Count} jump(s) relocated.");
        }

        foreach (var write in plan.Writes)
            ProtectedMemory.Write(game.AddressOf(write.Rva), relocation.BytesFor(write));

        Kernel32.FlushInstructionCache(Kernel32.GetCurrentProcess(), 0, 0);
        log.Info($"Game code patched: {plan.Writes.Count} change(s), {plan.PatchedByteCount} byte(s), game loaded at 0x{imageBase:X8} (built for 0x{plan.PreferredBase:X8}).");
    }

    private static uint ReserveNewSections(uint expectedAddress, uint size)
    {
        var aligned = (expectedAddress + AllocationGranularity - 1) / AllocationGranularity * AllocationGranularity;

        foreach (var candidate in new[] { expectedAddress, aligned })
        {
            var reserved = TryReserveAt(candidate, size);

            if (reserved != 0)
                return reserved;
        }

        var anywhere = Kernel32.VirtualAlloc(0, size, Kernel32.MemoryReserve | Kernel32.MemoryCommit, Kernel32.PageExecuteReadWrite);

        if (anywhere == 0)
            throw new PatchFailedException($"Could not reserve 0x{size:X} bytes for the mod's new code (error {Marshal.GetLastPInvokeError()}).");

        return (uint)anywhere;
    }

    private static uint TryReserveAt(uint address, uint size)
    {
        var reserved = Kernel32.VirtualAlloc((nint)address, size, Kernel32.MemoryReserve | Kernel32.MemoryCommit, Kernel32.PageExecuteReadWrite);

        if (reserved == (nint)address)
            return address;

        if (reserved != 0)
            Kernel32.VirtualFree(reserved, 0, Kernel32.MemoryRelease);

        return 0;
    }
}
