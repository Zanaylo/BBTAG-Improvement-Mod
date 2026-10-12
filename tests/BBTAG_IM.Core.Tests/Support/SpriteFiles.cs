using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal static class SpriteFiles
{
    public const int FpacPrefixLength = 0x20;
    public const int FpacNameLength = 20;
    public const int FpacEntryLength = 48;
    public const int HipDataStart = 0x440;
    public const int HplHeaderLength = 0x20;
    public const int HplColorsLength = 0x400;

    public static byte[] Fpac(params (string Name, byte[] Bytes)[] files)
    {
        int dataOffset = FpacPrefixLength + files.Length * FpacEntryLength;
        int total = dataOffset + files.Sum(file => file.Bytes.Length);
        byte[] bytes = new byte[total];
        Encoding.ASCII.GetBytes("FPAC").CopyTo(bytes, 0);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), dataOffset);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), total);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), files.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), 1);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(20), FpacNameLength);

        int offset = 0;

        for (int index = 0; index < files.Length; index++)
        {
            int entry = FpacPrefixLength + index * FpacEntryLength;
            Encoding.ASCII.GetBytes(files[index].Name).CopyTo(bytes, entry);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(entry + FpacNameLength), index);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(entry + FpacNameLength + 4), offset);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(entry + FpacNameLength + 8), files[index].Bytes.Length);
            files[index].Bytes.CopyTo(bytes, dataOffset + offset);
            offset += files[index].Bytes.Length;
        }

        return bytes;
    }

    public static byte[] Hpl(byte fill)
    {
        byte[] bytes = new byte[HplHeaderLength + HplColorsLength];
        Encoding.ASCII.GetBytes("HPAL").CopyTo(bytes, 0);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 0x125);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), bytes.Length);
        Array.Fill(bytes, fill, HplHeaderLength, HplColorsLength);
        return bytes;
    }

    public static byte[] Hip(int width, int height, params (byte Index, byte Run)[] runs)
    {
        byte[] bytes = new byte[HipDataStart + runs.Length * 2];
        Encoding.ASCII.GetBytes("HIP").CopyTo(bytes, 0);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 0x125);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), bytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), 256);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), width);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(20), height);
        bytes[24] = 1;
        bytes[25] = 1;
        bytes[26] = 1;
        bytes[27] = 0x20;
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28), 0x20);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(32), width);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(36), height);

        for (int run = 0; run < runs.Length; run++)
        {
            bytes[HipDataStart + run * 2] = runs[run].Index;
            bytes[HipDataStart + run * 2 + 1] = runs[run].Run;
        }

        return bytes;
    }
}
