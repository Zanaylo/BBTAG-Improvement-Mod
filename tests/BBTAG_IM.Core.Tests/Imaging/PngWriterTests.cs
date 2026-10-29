using System.Buffers.Binary;
using System.IO.Compression;
using BBTAG_IM.Core.Imaging;

namespace BBTAG_IM.Core.Tests.Imaging;

public class PngWriterTests
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void Encode_Image_StartsWithTheSignatureAndTheHeader()
    {
        // Act
        byte[] png = PngWriter.Encode(2, 1, [1, 2, 3, 255, 4, 5, 6, 255], 8);

        // Assert
        Assert.Equal(Signature, png[..8]);
        Assert.Equal("IHDR"u8.ToArray(), png[12..16]);
        Assert.Equal((2, 1, (byte)8, (byte)2), (BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)), png[24], png[25]));
    }

    [Fact]
    public void Encode_Image_EndsWithTheStandardEndChunk()
    {
        // Act
        byte[] png = PngWriter.Encode(1, 1, [0, 0, 0, 255], 4);

        // Assert
        Assert.Equal([0, 0, 0, 0, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82], png[^12..]);
    }

    [Fact]
    public void Encode_BlueGreenRedPixels_StoresRgbRowsWithoutPadding()
    {
        // Arrange
        byte[] pixels = [10, 20, 30, 255, 40, 50, 60, 255, 99, 99];

        // Act
        byte[] png = PngWriter.Encode(2, 1, pixels, 10);

        // Assert
        Assert.Equal([0, 30, 20, 10, 60, 50, 40], Rows(png));
    }

    [Fact]
    public void Encode_HeaderChunk_HasTheRightChecksum()
    {
        // Act
        byte[] png = PngWriter.Encode(1, 1, [0, 0, 0, 255], 4);

        // Assert
        Assert.Equal(0x907753DEu, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(29)));
    }

    private static byte[] Rows(byte[] png)
    {
        int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(33));
        using ZLibStream zlib = new(new MemoryStream(png, 41, length), CompressionMode.Decompress);
        using MemoryStream rows = new();
        zlib.CopyTo(rows);
        return rows.ToArray();
    }
}
