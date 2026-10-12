using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Tests.Patching;

public class LinearCodeScannerTests
{
    private const uint CodeRva = 0x1000;

    private static readonly byte[] Code =
    [
        0x55,
        0x8B, 0xEC,
        0xB8, 0x00, 0x00, 0x40, 0x01,
        0x8D, 0x15, 0x40, 0x10, 0x29, 0x02,
        0x8B, 0x84, 0x87, 0xD8, 0xD1, 0x08, 0x00,
        0x8B, 0x0C, 0xBD, 0x40, 0x10, 0x29, 0x02,
        0xE8, 0x00, 0x00, 0x00, 0x00,
        0x74, 0x01,
        0xC3,
    ];

    [Fact]
    public void FieldsOverlapping_ReturnsOperandsOfInstructionsTouchingTheWindow()
    {
        var fields = LinearCodeScanner.FieldsOverlapping(Code, CodeRva, new AddressRange(0x1003, 0x0B));

        Assert.Equal(
            [new OperandField(0x1004, 0x01400000, false), new OperandField(0x100A, 0x02291040, true)],
            fields.Operands);
        Assert.Empty(fields.Branches);
    }

    [Fact]
    public void FieldsOverlapping_FlagsOnlyBaselessDisplacementsAsAbsoluteMemory()
    {
        var fields = LinearCodeScanner.FieldsOverlapping(Code, CodeRva, new AddressRange(0x1010, 0x0C));

        Assert.Equal(
            [new OperandField(0x1011, 0x0008D1D8, false), new OperandField(0x1018, 0x02291040, true)],
            fields.Operands);
    }

    [Fact]
    public void FieldsOverlapping_ReportsRel32BranchesSeparately()
    {
        var fields = LinearCodeScanner.FieldsOverlapping(Code, CodeRva, new AddressRange(0x101C, 0x08));

        Assert.Empty(fields.Operands);
        Assert.Equal([new BranchField(0x101D, 0x1021)], fields.Branches);
    }
}
