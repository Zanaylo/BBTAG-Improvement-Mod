namespace BBTAG_IM.Core.Patching;

public readonly record struct OperandField(uint Rva, uint Value, bool IsAbsoluteMemory);

public readonly record struct BranchField(uint Rva, uint Target);

public sealed record DecodedFields(IReadOnlyList<OperandField> Operands, IReadOnlyList<BranchField> Branches);

public sealed record CaveFields(IReadOnlyList<int> AddressFields, IReadOnlyList<int> BranchFields);
