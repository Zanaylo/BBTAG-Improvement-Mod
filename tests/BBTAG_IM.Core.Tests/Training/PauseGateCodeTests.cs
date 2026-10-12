using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Training;

namespace BBTAG_IM.Core.Tests.Training;

public class PauseGateCodeTests
{
    private const uint CodeStart = 0x006BC2A0;
    private const uint ZeroReturn = 0x006BC2C8;
    private const uint Block = 0x10000000;

    private static readonly byte[] UpdateCheckEnd =
    [
        0x8B, 0xC8, 0xE8, 0x19, 0x71, 0x20, 0x00, 0x85, 0xC0, 0x75, 0x96, 0xE9, 0x00, 0x00, 0x00, 0x00,
        0x83, 0x3D, 0x80, 0xC0, 0x14, 0x01, 0x00, 0x74, 0x0F, 0x83, 0x3D, 0xAC, 0x88, 0xBD, 0x00, 0x00,
        0x75, 0x06, 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x33, 0xC0, 0xC3, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC,
    ];

    [Fact]
    public void Locate_GameCode_ReturnsWhereTheCheckSaysTheMatchMayRun()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, UpdateCheckEnd)]);

        // Act
        uint site = PauseGateCode.Locate(image);

        // Assert
        Assert.Equal(ZeroReturn, site);
    }

    [Fact]
    public void Locate_CodeWithoutTheCheck_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => PauseGateCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void Code_Block_CountsTheCallAndReturnsTheHoldFlag()
    {
        // Arrange
        byte[] expected = [0xFF, 0x05, 0x04, 0x00, 0x00, 0x10, 0xA1, 0x00, 0x00, 0x00, 0x10, 0xC3];

        // Act
        byte[] code = PauseGateCode.Code(Block);

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void Code_Block_FitsInTheBlock()
    {
        // Arrange
        int end = PauseGateCode.CodeOffset;

        // Act
        end += PauseGateCode.Code(Block).Length;

        // Assert
        Assert.True(end <= PauseGateCode.BlockLength);
    }

    [Fact]
    public void Jump_SiteAndBlock_JumpsToTheCodeOfTheBlock()
    {
        // Arrange
        uint distance = Block + PauseGateCode.CodeOffset - (ZeroReturn + 5);

        // Act
        byte[] jump = PauseGateCode.Jump(ZeroReturn, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(distance)], jump);
    }
}
