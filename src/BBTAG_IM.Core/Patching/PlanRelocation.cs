using System.Buffers.Binary;

namespace BBTAG_IM.Core.Patching;

public sealed class PlanRelocation(ExePatchPlan plan, uint imageBase, uint newSectionsAddress)
{
    private const uint FieldSize = sizeof(uint);

    private readonly uint _delta = imageBase - plan.PreferredBase;
    private readonly uint _shift = newSectionsAddress - (imageBase + plan.NewSectionRvas.Start);

    public byte[] BytesFor(PatchWrite write) => Relocate(write.Bytes, write.Rva, write.AddressFields, write.BranchFields);

    public byte[] BytesFor(PatchSection section) => Relocate(section.Bytes, section.Rva, section.AddressFields, section.BranchFields);

    public uint AddressOf(PatchSection section) => newSectionsAddress + (section.Rva - plan.NewSectionRvas.Start);

    private byte[] Relocate(byte[] bytes, uint rva, IReadOnlyList<int> addressFields, IReadOnlyList<int> branchFields)
    {
        var relocated = bytes.ToArray();

        foreach (var field in addressFields)
            Adjust(relocated, field, AddressAdjustment);

        foreach (var field in branchFields)
            Adjust(relocated, field, displacement => BranchAdjustment(rva + (uint)field, displacement));

        return relocated;
    }

    private uint AddressAdjustment(uint address) =>
        plan.NewSectionRvas.Contains(address - plan.PreferredBase) ? _delta + _shift : _delta;

    private uint BranchAdjustment(uint fieldRva, uint displacement)
    {
        var leavesNewSections = plan.NewSectionRvas.Contains(fieldRva);
        var entersNewSections = plan.NewSectionRvas.Contains(fieldRva + FieldSize + displacement);

        if (leavesNewSections == entersNewSections)
            return 0;

        return entersNewSections ? _shift : 0u - _shift;
    }

    private static void Adjust(byte[] bytes, int field, Func<uint, uint> adjustmentFor)
    {
        var span = bytes.AsSpan(field, (int)FieldSize);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span, value + adjustmentFor(value));
    }
}
