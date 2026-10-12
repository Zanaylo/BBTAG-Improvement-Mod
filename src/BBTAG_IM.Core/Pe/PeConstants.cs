namespace BBTAG_IM.Core.Pe;

public static class PeConstants
{
    public const ushort DosSignature = 0x5A4D;
    public const uint NtSignature = 0x00004550;
    public const ushort MachineI386 = 0x014C;
    public const ushort OptionalHeader32Magic = 0x010B;

    public const int DosNewHeaderOffsetField = 0x3C;
    public const int NtSignatureSize = 4;
    public const int FileHeaderSize = 20;
    public const int SectionHeaderSize = 40;
    public const int SectionNameSize = 8;
    public const int ImportDescriptorSize = 20;
    public const int DataDirectorySize = 8;
    public const int AddressSize = sizeof(uint);

    public const int ExportDirectoryIndex = 0;
    public const int ImportDirectoryIndex = 1;
    public const int BaseRelocationDirectoryIndex = 5;

    public const ushort FileRelocsStripped = 0x0001;
    public const uint SectionExecutable = 0x20000000;
    public const uint ImportByOrdinalFlag = 0x80000000;

    public const int RelocationTypeShift = 12;
    public const ushort RelocationOffsetMask = 0x0FFF;
    public const int RelocationTypeHighLow = 3;
    public const int RelocationBlockHeaderSize = 8;
}
