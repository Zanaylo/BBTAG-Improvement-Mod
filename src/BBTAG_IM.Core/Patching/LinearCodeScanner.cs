using Iced.Intel;

namespace BBTAG_IM.Core.Patching;

public static class LinearCodeScanner
{
    public static DecodedFields FieldsOverlapping(byte[] code, uint codeRva, AddressRange window)
    {
        var reader = new ByteArrayCodeReader(code);
        var decoder = Decoder.Create(InstructionOperands.Bitness, reader, codeRva);
        var operands = new List<OperandField>();
        var branches = new List<BranchField>();

        while (reader.CanReadByte)
        {
            decoder.Decode(out var instruction);

            if (instruction.IP >= window.End)
                break;

            if (instruction.IsInvalid || instruction.NextIP <= window.Start)
                continue;

            var offsets = decoder.GetConstantOffsets(instruction);
            operands.AddRange(InstructionOperands.FieldsOf(instruction, offsets));

            if (InstructionOperands.BranchOf(instruction, offsets) is { } branch)
                branches.Add(branch);
        }

        return new DecodedFields(operands, branches);
    }
}
