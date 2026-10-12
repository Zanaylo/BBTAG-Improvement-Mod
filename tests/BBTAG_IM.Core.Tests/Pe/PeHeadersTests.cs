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
    public void Parse_32BitImage_ReadsTheFileAndOptionalHeaderFields()
    {
        // Arrange
        uint timeDateStamp = _faker.Random.UInt();
        TestPeBuilder builder = new TestPeBuilder().WithTimeDateStamp(timeDateStamp).WithImageBase(0x10000000).WithEntryPoint(0x5A3E10);
        builder.AddSection(".text", _faker.Random.Bytes(0x300), TestPeBuilder.CodeCharacteristics);

        // Act
        PeHeaders headers = PeHeaders.Parse(builder.Build());

        // Assert
        Assert.Equal(
            (PeConstants.MachineI386, timeDateStamp, 0x10000000u, 0x2000u, TestPeBuilder.HeadersSize, 0x5A3E10u, false),
            (headers.Machine, headers.TimeDateStamp, headers.ImageBase, headers.SizeOfImage, headers.SizeOfHeaders, headers.AddressOfEntryPoint, headers.RelocationsStripped));
    }

    [Fact]
    public void Parse_CodeAndDataSections_ReadsEverySection()
    {
        // Arrange
        TestPeBuilder builder = new();
        builder.AddSection(".text", _faker.Random.Bytes(0x1200), TestPeBuilder.CodeCharacteristics);
        builder.AddSection(".data", _faker.Random.Bytes(0x10), virtualSize: 0x5000);

        // Act
        IReadOnlyList<PeSection> sections = PeHeaders.Parse(builder.Build()).Sections;

        // Assert
        Assert.Equal(
            [
                new PeSection(".text", 0x1000, 0x1200, 0x400, 0x1200, TestPeBuilder.CodeCharacteristics),
                new PeSection(".data", 0x3000, 0x5000, 0x1600, 0x200, TestPeBuilder.DataCharacteristics),
            ],
            sections);
        Assert.Equal([true, false], sections.Select(section => section.IsExecutable));
    }

    [Fact]
    public void Parse_ImageWithImportDirectoryOnly_ReadsItAndReportsTheExportsMissing()
    {
        // Arrange
        TestPeBuilder builder = new TestPeBuilder().WithDirectory(PeConstants.ImportDirectoryIndex, 0x2000, 0x28);
        builder.AddSection(".idata", new byte[0x40]);

        // Act
        PeHeaders headers = PeHeaders.Parse(builder.Build());

        // Assert
        Assert.Equal(new PeDataDirectory(0x2000, 0x28), headers.Directory(PeConstants.ImportDirectoryIndex));
        Assert.False(headers.Directory(PeConstants.ExportDirectoryIndex).IsPresent);
    }

    [Fact]
    public void Parse_RelocationsStrippedFlag_ReportsIt()
    {
        // Arrange
        byte[] image = new TestPeBuilder().WithFileCharacteristics(0x0103).Build();

        // Act
        PeHeaders headers = PeHeaders.Parse(image);

        // Assert
        Assert.True(headers.RelocationsStripped);
    }

    [Theory]
    [MemberData(nameof(BrokenImages))]
    public void Parse_ImageThatIsNotA32BitPe_ThrowsInvalidPeImage(byte[] image)
    {
        // Act
        Exception? exception = Record.Exception(() => PeHeaders.Parse(image));

        // Assert
        Assert.IsType<InvalidPeImageException>(exception);
    }

    public static TheoryData<byte[]> BrokenImages() =>
    [
        Edited(image => image[0] = 0),
        Edited(image => image[TestPeBuilder.PeHeaderOffset] = 0),
        new TestPeBuilder().WithMachine(MachineAmd64).Build(),
        new TestPeBuilder().WithOptionalHeaderMagic(Pe32PlusMagic).Build(),
        new TestPeBuilder().Build().AsSpan(0, 0x90).ToArray(),
    ];

    private static byte[] Edited(Action<byte[]> edit)
    {
        byte[] image = new TestPeBuilder().Build();
        edit(image);
        return image;
    }
}
