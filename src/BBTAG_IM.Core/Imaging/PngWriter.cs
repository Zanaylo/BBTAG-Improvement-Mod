using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace BBTAG_IM.Core.Imaging;

public static class PngWriter
{
    private const int BytesPerSourcePixel = 4;
    private const int BytesPerPixel = 3;
    private const byte BitDepth = 8;
    private const byte TrueColor = 2;
    private const byte NoFilter = 0;
    private const uint CrcPolynomial = 0xEDB88320;

    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(int width, int height, ReadOnlySpan<byte> bgra, int pitch)
    {
        using var file = new MemoryStream();
        file.Write(Signature);
        WriteChunk(file, "IHDR", Header(width, height));
        WriteChunk(file, "IDAT", CompressedRows(width, height, bgra, pitch));
        WriteChunk(file, "IEND", []);
        return file.ToArray();
    }

    private static byte[] Header(int width, int height)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = BitDepth;
        header[9] = TrueColor;
        return header;
    }

    private static byte[] CompressedRows(int width, int height, ReadOnlySpan<byte> bgra, int pitch)
    {
        var row = new byte[1 + width * BytesPerPixel];
        using var compressed = new MemoryStream();

        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            for (var y = 0; y < height; y++)
            {
                row[0] = NoFilter;
                var source = bgra.Slice(y * pitch, width * BytesPerSourcePixel);

                for (var x = 0; x < width; x++)
                {
                    row[1 + x * BytesPerPixel] = source[x * BytesPerSourcePixel + 2];
                    row[2 + x * BytesPerPixel] = source[x * BytesPerSourcePixel + 1];
                    row[3 + x * BytesPerPixel] = source[x * BytesPerSourcePixel];
                }

                zlib.Write(row);
            }
        }

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream file, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[sizeof(uint)];
        var typeBytes = Encoding.ASCII.GetBytes(type);

        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        file.Write(number);
        file.Write(typeBytes);
        file.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(number, Crc([.. typeBytes, .. data]));
        file.Write(number);
    }

    private static uint Crc(ReadOnlySpan<byte> bytes)
    {
        var crc = uint.MaxValue;

        foreach (var value in bytes)
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);

        return ~crc;
    }

    private static uint[] BuildCrcTable() =>
        Enumerable.Range(0, 256).Select(index =>
        {
            var value = (uint)index;

            for (var bit = 0; bit < 8; bit++)
                value = (value & 1) != 0 ? CrcPolynomial ^ (value >> 1) : value >> 1;

            return value;
        }).ToArray();
}
