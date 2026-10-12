using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Pe;

public class RelocationTableTests
{
    [Fact]
    public void ReadAddressSites_HighLowEntriesOnSeveralPages_ReturnsEverySite()
    {
        // Arrange
        uint[] sites = [0x1004, 0x1ffc, 0x2010, 0x2014, 0x5000];
        TestPeBuilder builder = new();
        builder.AddSection(".text", new byte[0x5000], TestPeBuilder.CodeCharacteristics);
        byte[] relocations = TestPeTables.Relocations(sites);
        uint rva = builder.AddSection(".reloc", relocations);
        builder.WithDirectory(PeConstants.BaseRelocationDirectoryIndex, rva, (uint)relocations.Length);
        FileImage image = new(builder.Build());

        // Act
        IReadOnlySet<uint> read = RelocationTable.ReadAddressSites(image, image.Headers);

        // Assert
        Assert.Equal(sites, read.Order());
    }

    [Fact]
    public void ReadAddressSites_ImageWithoutRelocationDirectory_ReturnsEmpty()
    {
        // Arrange
        FileImage image = new(new TestPeBuilder().Build());

        // Act
        IReadOnlySet<uint> read = RelocationTable.ReadAddressSites(image, image.Headers);

        // Assert
        Assert.Empty(read);
    }
}
