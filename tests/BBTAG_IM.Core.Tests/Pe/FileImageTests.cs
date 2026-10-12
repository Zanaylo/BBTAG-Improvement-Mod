using System.Buffers.Binary;
using System.Text;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;
using Bogus;

namespace BBTAG_IM.Core.Tests.Pe;

public class FileImageTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void FileOffsetOf_RvaInsideASection_ReturnsItsRawOffset()
    {
        // Arrange
        TestPeBuilder builder = new();
        builder.AddSection(".text", _faker.Random.Bytes(0x100), TestPeBuilder.CodeCharacteristics);
        uint dataRva = builder.AddSection(".data", _faker.Random.Bytes(0x100));
        FileImage image = new(builder.Build());

        // Act
        int? offset = image.FileOffsetOf(dataRva + 0x10);

        // Assert
        Assert.Equal(0x610, offset);
    }

    [Fact]
    public void FileOffsetOf_RvaInsideTheHeaders_ReturnsTheSameValue()
    {
        // Arrange
        FileImage image = new(new TestPeBuilder().Build());

        // Act
        int? offset = image.FileOffsetOf(0x3C);

        // Assert
        Assert.Equal(0x3C, offset);
    }

    [Theory]
    [InlineData(0x2000)]
    [InlineData(0x50000)]
    public void FileOffsetOf_RvaWithoutRawData_ReturnsNull(uint distanceFromSection)
    {
        // Arrange
        TestPeBuilder builder = new();
        uint rva = builder.AddSection(".bss", new byte[0x10], virtualSize: 0x3000);
        FileImage image = new(builder.Build());

        // Act
        int? offset = image.FileOffsetOf(rva + distanceFromSection);

        // Assert
        Assert.Null(offset);
    }

    [Fact]
    public void ReadUInt32_MappedRva_ReadsLittleEndian()
    {
        // Arrange
        uint value = _faker.Random.UInt();
        byte[] data = new byte[0x20];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), value);
        TestPeBuilder builder = new();
        uint rva = builder.AddSection(".data", data);
        FileImage image = new(builder.Build());

        // Act
        uint read = image.ReadUInt32(rva + 8);

        // Assert
        Assert.Equal(value, read);
    }

    [Fact]
    public void ReadUInt16_MappedRva_ReadsLittleEndian()
    {
        // Arrange
        ushort value = _faker.Random.UShort();
        byte[] data = new byte[0x20];
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(8), value);
        TestPeBuilder builder = new();
        uint rva = builder.AddSection(".data", data);
        FileImage image = new(builder.Build());

        // Act
        ushort read = image.ReadUInt16(rva + 8);

        // Assert
        Assert.Equal(value, read);
    }

    [Fact]
    public void ReadUInt32_UnmappedRva_ThrowsInvalidPeImage()
    {
        // Arrange
        FileImage image = new(new TestPeBuilder().Build());

        // Act
        Exception? exception = Record.Exception(() => image.ReadUInt32(0x9000));

        // Assert
        Assert.IsType<InvalidPeImageException>(exception);
    }

    [Fact]
    public void ReadAsciiString_TerminatedText_ReadsUpToTheTerminator()
    {
        // Arrange
        string text = _faker.Lorem.Word();
        TestPeBuilder builder = new();
        uint rva = builder.AddSection(".rdata", Encoding.ASCII.GetBytes("xx" + text + "\0tail"));
        FileImage image = new(builder.Build());

        // Act
        string read = image.ReadAsciiString(rva + 2);

        // Assert
        Assert.Equal(text, read);
    }

    [Fact]
    public void SectionData_Section_ReturnsItsRawBytes()
    {
        // Arrange
        byte[] data = _faker.Random.Bytes(0x200);
        TestPeBuilder builder = new();
        builder.AddSection(".text", data, TestPeBuilder.CodeCharacteristics);
        FileImage image = new(builder.Build());

        // Act
        byte[] read = image.SectionData(image.Headers.Sections[0]).ToArray();

        // Assert
        Assert.Equal(data, read);
    }
}
