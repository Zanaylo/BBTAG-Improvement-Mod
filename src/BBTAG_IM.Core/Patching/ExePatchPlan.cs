namespace BBTAG_IM.Core.Patching;

public sealed record PatchWrite(uint Rva, byte[] Bytes, IReadOnlyList<int> AddressFields, IReadOnlyList<int> BranchFields);

public sealed record PatchSection(
    string Name,
    uint Rva,
    uint VirtualSize,
    byte[] Bytes,
    bool IsExecutable,
    IReadOnlyList<int> AddressFields,
    IReadOnlyList<int> BranchFields);

public sealed record ExePatchPlan(
    uint PreferredBase,
    AddressRange NewSectionRvas,
    IReadOnlyList<PatchWrite> Writes,
    IReadOnlyList<PatchSection> Sections)
{
    public bool IsEmpty => Writes.Count == 0 && Sections.Count == 0;

    public int PatchedByteCount => Writes.Sum(write => write.Bytes.Length);
}
