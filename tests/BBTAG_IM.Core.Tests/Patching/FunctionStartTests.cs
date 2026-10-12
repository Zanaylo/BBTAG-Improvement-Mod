using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Tests.Patching;

public class FunctionStartTests
{
    private const byte Int3 = 0xCC;
    private const uint AlignedRva = 0x1000;

    [Theory]
    [InlineData(0x30)]
    [InlineData(0x10)]
    public void AnchorBefore_OffsetAfterPaddedFunctionStart_ReturnsTheAlignedStart(int offset)
    {
        // Arrange
        byte[] code = TwoFunctions();

        // Act
        int anchor = FunctionStart.AnchorBefore(code, AlignedRva, offset);

        // Assert
        Assert.Equal(0x10, anchor);
    }

    [Fact]
    public void AnchorBefore_CodeNotAlignedInTheImage_UsesTheRvaAlignment()
    {
        // Arrange
        byte[] code = TwoFunctions();

        // Act
        int anchor = FunctionStart.AnchorBefore(code, 0x1008, 0x30);

        // Assert
        Assert.Equal(0, anchor);
    }

    [Theory]
    [InlineData(0xC0, 0xC0 - FunctionStart.FallbackLead)]
    [InlineData(0x20, 0)]
    public void AnchorBefore_NoPaddingBeforeTheOffset_FallsBackToAFixedLeadClampedAtZero(int offset, int expected)
    {
        // Arrange
        byte[] code = new byte[0x100];

        // Act
        int anchor = FunctionStart.AnchorBefore(code, AlignedRva, offset);

        // Assert
        Assert.Equal(expected, anchor);
    }

    private static byte[] TwoFunctions()
    {
        byte[] code = new byte[0x40];
        code[0x0D] = 0xC3;
        code[0x0E] = Int3;
        code[0x0F] = Int3;
        code[0x10] = 0x55;
        code[0x1F] = 0x8B;
        code[0x20] = 0x55;
        return code;
    }
}
