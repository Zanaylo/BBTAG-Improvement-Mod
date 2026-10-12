using Iced.Intel;

namespace BBTAG_IM.Core.Patching;

public static class CaveCodeScanner
{
    public static CaveFields FindFields(byte[] code, uint codeRva, IEnumerable<uint> entryRvas, AddressRange image)
    {
        var trace = new Trace(code, codeRva, image);

        foreach (var entry in entryRvas)
            trace.Pending.Push(entry);

        while (trace.Pending.TryPop(out var start))
            trace.FollowFrom(start);

        return new CaveFields(trace.AddressFields.ToArray(), trace.BranchFields.ToArray());
    }

    private sealed class Trace(byte[] code, uint codeRva, AddressRange image)
    {
        private readonly ByteArrayCodeReader _reader = new(code);
        private readonly AddressRange _code = new(codeRva, (uint)code.Length);
        private readonly HashSet<uint> _visited = [];
        private Decoder? _decoder;

        public Stack<uint> Pending { get; } = new();

        public SortedSet<int> AddressFields { get; } = [];

        public SortedSet<int> BranchFields { get; } = [];

        public void FollowFrom(uint rva)
        {
            _decoder ??= Decoder.Create(InstructionOperands.Bitness, _reader);

            while (_code.Contains(rva) && _visited.Add(rva))
            {
                _reader.Position = (int)(rva - codeRva);
                _decoder.IP = rva;
                _decoder.Decode(out var instruction);

                if (instruction.IsInvalid)
                    return;

                Record(instruction, _decoder.GetConstantOffsets(instruction));

                if (!ContinuesAfter(instruction))
                    return;

                rva = (uint)instruction.NextIP;
            }
        }

        private static bool ContinuesAfter(in Instruction instruction) =>
            instruction.FlowControl is not (FlowControl.UnconditionalBranch or FlowControl.Return or FlowControl.IndirectBranch
                or FlowControl.Interrupt or FlowControl.Exception);

        private void Record(Instruction instruction, ConstantOffsets offsets)
        {
            foreach (var field in InstructionOperands.FieldsOf(instruction, offsets))
            {
                if (image.Contains(field.Value))
                    AddressFields.Add(OffsetOf(field.Rva));
            }

            if (instruction.Op0Kind is not (OpKind.NearBranch16 or OpKind.NearBranch32))
                return;

            if (_code.Contains(instruction.NearBranch32))
            {
                Pending.Push(instruction.NearBranch32);
                return;
            }

            if (InstructionOperands.BranchOf(instruction, offsets) is { } branch)
                BranchFields.Add(OffsetOf(branch.Rva));
        }

        private int OffsetOf(uint rva) => (int)(rva - codeRva);
    }
}
