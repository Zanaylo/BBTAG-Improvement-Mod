using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Game;

public class SittingOutSpawnGateCodeTests
{
    private const uint CodeStart = 0x0076B919;
    private const uint Block = 0x10000000;
    private const uint Game = 0x00E59100;

    private static readonly byte[] CreateObjectHandler =
    [
        0x8B, 0x45, 0x08, 0xB9, 0x08, 0x00, 0x00, 0x00, 0x8B, 0x75, 0x08, 0x8B, 0x13, 0x0F, 0xB7, 0x40,
        0x20, 0x50, 0x83, 0xEC, 0x20, 0x8B, 0xFC, 0xF3, 0xA5, 0x8B, 0xCB, 0xFF, 0x92, 0x58, 0x05, 0x00,
        0x00, 0x33, 0xC0,
    ];

    [Fact]
    public void Locate_GameCode_ReturnsTheStartOfTheCreateObjectHandler()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, CreateObjectHandler)]);

        // Act
        uint site = SittingOutSpawnGateCode.Locate(image);

        // Assert
        Assert.Equal(CodeStart, site);
    }

    [Fact]
    public void Locate_CodeWithoutTheHandler_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => SittingOutSpawnGateCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void Code_SiteBlockAndGame_SkipsCreationForSlotsThatSitOutAndRunsItForTheOthers()
    {
        // Arrange
        uint resumeJumpEnd = Block + SittingOutSpawnGateCode.CodeOffset + 48;
        byte[] expected =
        [
            0x8B, 0x43, 0x30,
            0x83, 0xF8, 0x03,
            0x77, 0x1B,
            0x6B, 0xC0, 0x28,
            0x83, 0xB8, .. BitConverter.GetBytes(Game + 0x27C4), 0x01,
            0x75, 0x0F,
            0xFF, 0x05, .. BitConverter.GetBytes(Block + SittingOutSpawnGateCode.SkipsOffset),
            0x33, 0xC0, 0x5F, 0x5E, 0x5B, 0x5D, 0xC2, 0x04, 0x00,
            0x8B, 0x45, 0x08, 0xB9, 0x08, 0x00, 0x00, 0x00,
            0xE9, .. BitConverter.GetBytes(CodeStart + 8 - resumeJumpEnd),
        ];

        // Act
        byte[] code = SittingOutSpawnGateCode.Code(CodeStart, Block, Game);

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void Code_SiteBlockAndGame_FitsInTheBlock()
    {
        // Arrange
        int end = SittingOutSpawnGateCode.CodeOffset;

        // Act
        end += SittingOutSpawnGateCode.Code(CodeStart, Block, Game).Length;

        // Assert
        Assert.True(end <= SittingOutSpawnGateCode.BlockLength);
    }

    [Fact]
    public void Jump_SiteAndBlock_JumpsToTheCodeOverTheReplacedInstructions()
    {
        // Arrange
        uint distance = Block + SittingOutSpawnGateCode.CodeOffset - (CodeStart + 5);

        // Act
        byte[] jump = SittingOutSpawnGateCode.Jump(CodeStart, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(distance), 0x90, 0x90, 0x90], jump);
    }
}
