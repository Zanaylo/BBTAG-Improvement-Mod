using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;
using Bogus;

namespace BBTAG_IM.Core.Tests.Pe;

public class PeHeadersTests
{
    private const ushort Pe32PlusMagic = 0x020B;
    private const ushort MachineAmd64 = 0x8664;

    private readonly Faker _faker = new();

    [Fact]
    public void Parse_ReadsFileAndOptionalHeaderFields()
    {
        var timeDateStamp = _faker.Random.UInt();
        var builder = new TestPeBuilder().WithTimeDateStamp(timeDateStamp).WithImageBase(0x10000000);
        builder.AddSection(".text", _faker.Random.Bytes(0x300), TestPeBuilder.CodeCharacteristics);

        var headers = PeHeaders.Parse(builder.Build());

        Assert.Equal(PeConstants.MachineI386, headers.Machine);
        Assert.Equal(timeDateStamp, headers.TimeDateStamp);
        Assert.Equal(0x10000000u, headers.ImageBase);
        Assert.Equal(0x2000u, headers.SizeOfImage);
        Assert.Equal(TestPeBuilder.HeadersSize, headers.SizeOfHeaders);
        Assert.False(headers.RelocationsStripped);
    }

    [Fact]
    public void Parse_ReadsEntryPoint()
    {
        var image = new TestPeBuilder().WithEntryPoint(0x5A3E10).Build();

        Assert.Equal(0x5A3E10u, PeHeaders.Parse(image).AddressOfEntryPoint);
    }

    [Fact]
    public void Parse_ReadsEverySection()
    {
        var builder = new TestPeBuilder();
        builder.AddSection(".text", _faker.Random.Bytes(0x1200), TestPeBuilder.CodeCharacteristics);
        builder.AddSection(".data", _faker.Random.Bytes(0x10), virtualSize: 0x5000);

        var sections = PeHeaders.Parse(builder.Build()).Sections;

        Assert.Equal(2, sections.Count);
        Assert.Equal(new PeSection(".text", 0x1000, 0x1200, 0x400, 0x1200, TestPeBuilder.CodeCharacteristics), sections[0]);
        Assert.Equal(new PeSection(".data", 0x3000, 0x5000, 0x1600, 0x200, TestPeBuilder.DataCharacteristics), sections[1]);
        Assert.True(sections[0].IsExecutable);
        Assert.False(sections[1].IsExecutable);
    }

    [Fact]
    public void Parse_ReadsDataDirectories()
    {
        var builder = new TestPeBuilder().WithDirectory(PeConstants.ImportDirectoryIndex, 0x2000, 0x28);
        builder.AddSection(".idata", new byte[0x40]);

        var headers = PeHeaders.Parse(builder.Build());

        Assert.Equal(new PeDataDirectory(0x2000, 0x28), headers.Directory(PeConstants.ImportDirectoryIndex));
        Assert.False(headers.Directory(PeConstants.ExportDirectoryIndex).IsPresent);
    }

    [Fact]
    public void Parse_ReportsStrippedRelocations()
    {
        var image = new TestPeBuilder().WithFileCharacteristics(0x0103).Build();

        Assert.True(PeHeaders.Parse(image).RelocationsStripped);
    }

    [Fact]
    public void Parse_RejectsMissingDosSignature()
    {
        var image = new TestPeBuilder().Build();
        image[0] = 0;

        Assert.Throws<InvalidPeImageException>(() => PeHeaders.Parse(image));
    }

    [Fact]
    public void Parse_RejectsMissingNtSignature()
    {
        var image = new TestPeBuilder().Build();
        image[TestPeBuilder.PeHeaderOffset] = 0;

        Assert.Throws<InvalidPeImageException>(() => PeHeaders.Parse(image));
    }

    [Fact]
    public void Parse_RejectsNon32BitMachine()
    {
        var image = new TestPeBuilder().WithMachine(MachineAmd64).Build();

        Assert.Throws<InvalidPeImageException>(() => PeHeaders.Parse(image));
    }

    [Fact]
    public void Parse_RejectsPe32PlusOptionalHeader()
    {
        var image = new TestPeBuilder().WithOptionalHeaderMagic(Pe32PlusMagic).Build();

        Assert.Throws<InvalidPeImageException>(() => PeHeaders.Parse(image));
    }

    [Fact]
    public void Parse_RejectsTruncatedImage()
    {
        var image = new TestPeBuilder().Build().AsSpan(0, 0x90).ToArray();

        Assert.Throws<InvalidPeImageException>(() => PeHeaders.Parse(image));
    }
}
