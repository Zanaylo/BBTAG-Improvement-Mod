using Iced.Intel;

namespace BBTAG_IM.Core.Patching;

internal static class InstructionOperands
{
    public const int Bitness = 32;
    public const int MaximumInstructionLength = 15;

    private const int AddressSize = sizeof(uint);

    public static IEnumerable<OperandField> FieldsOf(Instruction instruction, ConstantOffsets offsets)
    {
        var instructionRva = (uint)instruction.IP;

        for (var operand = 0; operand < instruction.OpCount; operand++)
        {
            var kind = instruction.GetOpKind(operand);

            if (kind == OpKind.Memory && offsets.HasDisplacement && instruction.MemoryDisplSize == AddressSize)
                yield return new OperandField(
                    instructionRva + offsets.DisplacementOffset,
                    instruction.MemoryDisplacement32,
                    instruction.MemoryBase == Register.None);

            if (kind == OpKind.Immediate32 && offsets.HasImmediate)
                yield return new OperandField(instructionRva + offsets.ImmediateOffset, instruction.Immediate32, false);
        }
    }

    public static BranchField? BranchOf(Instruction instruction, ConstantOffsets offsets)
    {
        if (instruction.Op0Kind != OpKind.NearBranch32 || offsets.ImmediateSize != AddressSize)
            return null;

        return new BranchField((uint)instruction.IP + offsets.ImmediateOffset, instruction.NearBranch32);
    }
}
