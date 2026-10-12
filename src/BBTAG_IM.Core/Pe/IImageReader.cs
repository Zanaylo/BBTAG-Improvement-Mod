namespace BBTAG_IM.Core.Pe;

public interface IImageReader
{
    ushort ReadUInt16(uint rva);

    uint ReadUInt32(uint rva);

    string ReadAsciiString(uint rva);
}
