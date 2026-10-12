using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Tests.Patching;

public class FunctionStartTests
{
    private const byte Int3 = 0xCC;

    private static byte[] TwoFunctions()
    {
        var code = new byte[0x40];
        code[0x0D] = 0xC3;
        code[0x0E] = Int3;
        code[0x0F] = Int3;
        code[0x10] = 0x55;
        code[0x1F] = 0x8B;
        code[0x20] = 0x55;
        return code;
    }

    [Fact]
    public void AnchorBefore_ReturnsAlignedStartAfterPadding()
    {
        Assert.Equal(0x10, FunctionStart.AnchorBefore(TwoFunctions(), 0x1000, 0x30));
    }

    [Fact]
    public void AnchorBefore_ReturnsOffsetItselfWhenItIsAFunctionStart()
    {
        Assert.Equal(0x10, FunctionStart.AnchorBefore(TwoFunctions(), 0x1000, 0x10));
    }

    [Fact]
    public void AnchorBefore_UsesRvaAlignment()
    {
        Assert.Equal(0, FunctionStart.AnchorBefore(TwoFunctions(), 0x1008, 0x30));
    }

    [Fact]
    public void AnchorBefore_FallsBackToFixedLeadWithoutPadding()
    {
        var code = new byte[0x100];

        Assert.Equal(0xC0 - FunctionStart.FallbackLead, FunctionStart.AnchorBefore(code, 0x1000, 0xC0));
        Assert.Equal(0, FunctionStart.AnchorBefore(code, 0x1000, 0x20));
    }
}
