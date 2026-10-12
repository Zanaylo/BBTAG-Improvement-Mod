namespace BBTAG_IM.Core.Pe;

public readonly record struct PeDataDirectory(uint VirtualAddress, uint Size)
{
    public bool IsPresent => VirtualAddress != 0 && Size != 0;
}
