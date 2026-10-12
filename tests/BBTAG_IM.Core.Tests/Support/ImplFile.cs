using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal static class ImplFile
{
    public const int HeaderLength = 20;
    public const int DataLength = 8320;
    public const int FileLength = 1024;
    public const int FileCount = 8;
    public const int FirstFileOffset = HeaderLength + 128;

    public static byte[] Create(int characterIndex, string creator = "", string description = "", byte fill = 0x11)
    {
        var bytes = new byte[HeaderLength + DataLength];
        Encoding.ASCII.GetBytes("IMPL").CopyTo(bytes, 0);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), HeaderLength);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), DataLength);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(16), (short)characterIndex);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(18), -1);
        Encoding.ASCII.GetBytes("Stored name").CopyTo(bytes, HeaderLength);
        Encoding.ASCII.GetBytes(creator).CopyTo(bytes, HeaderLength + 32);
        Encoding.ASCII.GetBytes(description).CopyTo(bytes, HeaderLength + 64);
        Array.Fill(bytes, fill, FirstFileOffset, FileCount * FileLength);
        return bytes;
    }

    public static byte[] WithEmptyFile(this byte[] bytes, int file)
    {
        Array.Clear(bytes, FirstFileOffset + file * FileLength, FileLength);
        return bytes;
    }
}
