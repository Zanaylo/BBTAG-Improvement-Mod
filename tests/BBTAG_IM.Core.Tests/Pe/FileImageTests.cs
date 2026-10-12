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
    public void FileOffsetOf_MapsSectionRvaToRawOffset()
    {
        var builder = new TestPeBuilder();
        builder.AddSection(".text", _faker.Random.Bytes(0x100), TestPeBuilder.CodeCharacteristics);
        var dataRva = builder.AddSection(".data", _faker.Random.Bytes(0x100));

        var image = new FileImage(builder.Build());

        Assert.Equal(0x610, image.FileOffsetOf(dataRva + 0x10));
    }

    [Fact]
    public void FileOffsetOf_MapsHeaderRvaToItself()
    {
        var image = new FileImage(new TestPeBuilder().Build());

        Assert.Equal(0x3C, image.FileOffsetOf(0x3C));
    }

    [Fact]
    public void FileOffsetOf_ReturnsNullOutsideRawData()
    {
        var builder = new TestPeBuilder();
        var rva = builder.AddSection(".bss", new byte[0x10], virtualSize: 0x3000);

        var image = new FileImage(builder.Build());

        Assert.Null(image.FileOffsetOf(rva + 0x2000));
        Assert.Null(image.FileOffsetOf(0x50000));
    }

    [Fact]
    public void ReadUInt32_ReadsLittleEndianValueAtRva()
    {
        var value = _faker.Random.UInt();
        var data = new byte[0x20];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), value);
        var builder = new TestPeBuilder();
        var rva = builder.AddSection(".data", data);

        var image = new FileImage(builder.Build());

        Assert.Equal(value, image.ReadUInt32(rva + 8));
        Assert.Equal((ushort)value, image.ReadUInt16(rva + 8));
    }

    [Fact]
    public void ReadUInt32_RejectsUnmappedRva()
    {
        var image = new FileImage(new TestPeBuilder().Build());

        Assert.Throws<InvalidPeImageException>(() => image.ReadUInt32(0x9000));
    }

    [Fact]
    public void ReadAsciiString_ReadsUntilTerminator()
    {
        var text = _faker.Lorem.Word();
        var builder = new TestPeBuilder();
        var rva = builder.AddSection(".rdata", Encoding.ASCII.GetBytes("xx" + text + "\0tail"));

        var image = new FileImage(builder.Build());

        Assert.Equal(text, image.ReadAsciiString(rva + 2));
    }

    [Fact]
    public void SectionData_ReturnsRawBytes()
    {
        var data = _faker.Random.Bytes(0x200);
        var builder = new TestPeBuilder();
        builder.AddSection(".text", data, TestPeBuilder.CodeCharacteristics);

        var image = new FileImage(builder.Build());

        Assert.Equal(data, image.SectionData(image.Headers.Sections[0]).ToArray());
    }
}
