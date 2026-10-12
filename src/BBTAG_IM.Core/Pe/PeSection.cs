namespace BBTAG_IM.Core.Pe;

public sealed record PeSection(
    string Name,
    uint VirtualAddress,
    uint VirtualSize,
    uint RawDataPointer,
    uint RawDataSize,
    uint Characteristics)
{
    public bool IsExecutable => (Characteristics & PeConstants.SectionExecutable) != 0;

    public uint MappedSize => Math.Max(VirtualSize, RawDataSize);

    public bool ContainsRva(uint rva) => rva >= VirtualAddress && rva - VirtualAddress < MappedSize;

    public bool HasSameLayoutAs(PeSection other) =>
        VirtualAddress == other.VirtualAddress && RawDataPointer == other.RawDataPointer;
}
