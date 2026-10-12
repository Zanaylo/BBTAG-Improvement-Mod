using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Tests.Patching;

public class CaveCodeScannerTests
{
    private const uint CodeRva = 0x5000;
    private const int UnreachableEntry = 43;

    private static readonly AddressRange Image = new(0x400000, 0x10000);

    private static readonly byte[] Code =
    [
        0x8D, 0x2D, 0x00, 0x51, 0x40, 0x00,
        0x8B, 0x44, 0xCD, 0xDC,
        0xA1, 0x04, 0x51, 0x40, 0x00,
        0xB8, 0x10, 0x00, 0x00, 0x00,
        0x68, 0x00, 0x10, 0x40, 0x00,
        0x74, 0x05,
        0xE9, 0xE0, 0xBF, 0xFF, 0xFF,
        0xC7, 0x05, 0x08, 0x51, 0x40, 0x00, 0x00, 0x10, 0x40, 0x00,
        0xC3,
        0xA1, 0x0C, 0x51, 0x40, 0x00,
        0xE8, 0xEB, 0xFF, 0xFF, 0xFF,
        0xC3,
    ];

    [Fact]
    public void FindFields_EntryAtTheStart_FindsAbsoluteOperandsOfReachableCodeOnly()
    {
        // Act
        CaveFields fields = CaveCodeScanner.FindFields(Code, CodeRva, [CodeRva], Image);

        // Assert
        Assert.Equal([2, 11, 21, 34, 38], fields.AddressFields);
    }

    [Fact]
    public void FindFields_EntryAtTheStart_ReportsRel32BranchesLeavingTheCode()
    {
        // Act
        CaveFields fields = CaveCodeScanner.FindFields(Code, CodeRva, [CodeRva], Image);

        // Assert
        Assert.Equal([28], fields.BranchFields);
    }

    [Fact]
    public void FindFields_SecondEntryIntoUnreachableCode_ScansFromEveryEntry()
    {
        // Act
        CaveFields fields = CaveCodeScanner.FindFields(Code, CodeRva, [CodeRva, CodeRva + UnreachableEntry], Image);

        // Assert
        Assert.Equal([2, 11, 21, 34, 38, 44], fields.AddressFields);
        Assert.Equal([28], fields.BranchFields);
    }

    [Fact]
    public void FindFields_ImageElsewhereInMemory_IgnoresTheAddresses()
    {
        // Act
        CaveFields fields = CaveCodeScanner.FindFields(Code, CodeRva, [CodeRva], new AddressRange(0x10000000, 0x1000));

        // Assert
        Assert.Empty(fields.AddressFields);
    }

    [Fact]
    public void FindFields_EntriesOutsideTheCode_FindsNothing()
    {
        // Act
        CaveFields fields = CaveCodeScanner.FindFields(Code, CodeRva, [0x1000, CodeRva + 0x1000], Image);

        // Assert
        Assert.Empty(fields.AddressFields);
        Assert.Empty(fields.BranchFields);
    }
}
