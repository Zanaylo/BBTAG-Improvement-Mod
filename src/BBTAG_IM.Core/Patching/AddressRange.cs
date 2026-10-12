namespace BBTAG_IM.Core.Patching;

public readonly record struct AddressRange(uint Start, uint Length)
{
    public uint End => Start + Length;

    public bool Contains(uint address) => address >= Start && address - Start < Length;
}
