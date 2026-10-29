using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Tests.Game;

public class FatalErrorPolicyTests
{
    private const uint AccessViolation = 0xC0000005;
    private const uint ModuleStart = 0x00D40000;
    private const uint ModuleEnd = 0x02B00000;
    private const uint InGame = 0x010A9260;

    [Theory]
    [InlineData(0xC0000005u)]
    [InlineData(0xC000001Du)]
    [InlineData(0xC0000094u)]
    [InlineData(0xC0000096u)]
    [InlineData(0xC000008Cu)]
    public void CanRecover_FatalCodeInGameCodeOnAnArmedThread_ReturnsTrue(uint code)
    {
        // Act
        bool recover = FatalErrorPolicy.CanRecover(code, InGame, ModuleStart, ModuleEnd, armedOnThisThread: true);

        // Assert
        Assert.True(recover);
    }

    [Fact]
    public void CanRecover_BreakpointOrOtherNonFatalCode_ReturnsFalse()
    {
        // Act
        bool recover = FatalErrorPolicy.CanRecover(0x80000003, InGame, ModuleStart, ModuleEnd, armedOnThisThread: true);

        // Assert
        Assert.False(recover);
    }

    [Fact]
    public void CanRecover_FaultOutsideTheGameModule_ReturnsFalse()
    {
        // Act
        bool recover = FatalErrorPolicy.CanRecover(AccessViolation, 0x75001000, ModuleStart, ModuleEnd, armedOnThisThread: true);

        // Assert
        Assert.False(recover);
    }

    [Fact]
    public void CanRecover_NoArmedStepOnThisThread_ReturnsFalse()
    {
        // Act
        bool recover = FatalErrorPolicy.CanRecover(AccessViolation, InGame, ModuleStart, ModuleEnd, armedOnThisThread: false);

        // Assert
        Assert.False(recover);
    }

    [Fact]
    public void Describe_AccessViolation_NamesItAndTheModuleOffset()
    {
        // Act
        string text = FatalErrorPolicy.Describe(AccessViolation, InGame, ModuleStart);

        // Assert
        Assert.Equal("access violation (0xC0000005) at BBTAG.exe+0x369260", text);
    }

    [Fact]
    public void Describe_UnknownCode_ShowsTheCode()
    {
        // Act
        string text = FatalErrorPolicy.Describe(0xC000008C, InGame, ModuleStart);

        // Assert
        Assert.Equal("array bounds exceeded (0xC000008C) at BBTAG.exe+0x369260", text);
    }
}
