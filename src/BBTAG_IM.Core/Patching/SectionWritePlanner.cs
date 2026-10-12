using System.Buffers.Binary;
using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Core.Patching;

internal sealed record PlanContext(IReadOnlySet<uint> RelocationSites, AddressRange Image, AddressRange NewSectionRvas)
{
    public AddressRange NewSections => NewSectionRvas with { Start = Image.Start + NewSectionRvas.Start };
}

internal sealed class SectionWritePlanner(byte[] stock, byte[] modded, PeSection section, PlanContext context)
{
    private readonly int _length = ComparableLength(stock, modded, section);
    private readonly HashSet<int> _addressFields = [];
    private readonly HashSet<int> _branchFields = [];

    public IReadOnlyList<PatchWrite> Plan()
    {
        var runs = ByteRuns.Merge(ByteRuns.Changed(stock.AsSpan(0, _length), modded.AsSpan(0, _length))
            .Select(run => ByteRuns.WidenOverFields(run, IsRelocationSite, _length)));

        if (section.IsExecutable)
            CollectCodeFields(runs);
        else
            CollectDataPointers(runs);

        runs = ByteRuns.Merge(runs.Select(run => ByteRuns.WidenOverFields(run, IsCollectedField, _length)));

        return runs.Select(WriteFor).ToArray();
    }

    private static int ComparableLength(byte[] stock, byte[] modded, PeSection section)
    {
        var length = Math.Min(stock.Length, modded.Length);
        return section.VirtualSize == 0 ? length : Math.Min(length, (int)section.VirtualSize);
    }

    private bool IsRelocationSite(int offset) => context.RelocationSites.Contains(section.VirtualAddress + (uint)offset);

    private bool IsCollectedField(int offset) => _addressFields.Contains(offset) || _branchFields.Contains(offset);

    private void CollectCodeFields(IEnumerable<ByteRun> runs)
    {
        foreach (var run in runs)
        {
            var anchor = FunctionStart.AnchorBefore(stock.AsSpan(0, _length), section.VirtualAddress, run.Start);
            var end = Math.Min(_length, run.End + InstructionOperands.MaximumInstructionLength);
            var window = new AddressRange(section.VirtualAddress + (uint)run.Start, (uint)run.Length);
            var fields = LinearCodeScanner.FieldsOverlapping(modded[anchor..end], section.VirtualAddress + (uint)anchor, window);

            foreach (var operand in fields.Operands.Where(IsCodeAddress))
                _addressFields.Add(OffsetOf(operand.Rva));

            foreach (var branch in fields.Branches.Where(branch => context.NewSectionRvas.Contains(branch.Target)))
                _branchFields.Add(OffsetOf(branch.Rva));
        }
    }

    private bool IsCodeAddress(OperandField operand) =>
        context.NewSections.Contains(operand.Value) || (operand.IsAbsoluteMemory && context.Image.Contains(operand.Value));

    private void CollectDataPointers(IEnumerable<ByteRun> runs)
    {
        foreach (var run in runs)
        {
            var firstAligned = AlignDown(section.VirtualAddress + (uint)Math.Max(0, run.Start - ByteRuns.AddressSize + 1)) - section.VirtualAddress;

            for (var offset = (int)firstAligned; offset < run.End && offset + ByteRuns.AddressSize <= _length; offset += ByteRuns.AddressSize)
            {
                if (context.NewSections.Contains(DwordAt(offset)))
                    _addressFields.Add(offset);
            }
        }
    }

    private static uint AlignDown(uint rva) => rva / ByteRuns.AddressSize * ByteRuns.AddressSize;

    private int OffsetOf(uint rva) => (int)(rva - section.VirtualAddress);

    private PatchWrite WriteFor(ByteRun run)
    {
        var fieldStarts = Enumerable.Range(run.Start, Math.Max(0, run.Length - ByteRuns.AddressSize + 1)).ToArray();
        var addressFields = fieldStarts
            .Where(offset => _addressFields.Contains(offset) || (IsRelocationSite(offset) && context.Image.Contains(DwordAt(offset))))
            .Select(offset => offset - run.Start)
            .ToArray();
        var branchFields = fieldStarts
            .Where(_branchFields.Contains)
            .Select(offset => offset - run.Start)
            .ToArray();

        return new PatchWrite(section.VirtualAddress + (uint)run.Start, modded[run.Start..run.End], addressFields, branchFields);
    }

    private uint DwordAt(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(modded.AsSpan(offset));
}
