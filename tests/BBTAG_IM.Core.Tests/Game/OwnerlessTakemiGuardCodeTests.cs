using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Game;

public class OwnerlessTakemiGuardCodeTests
{
    private const uint CodeStart = 0x0079DEA0;
    private const uint Block = 0x10000000;

    private static readonly byte[] CollisionHandlerStart =
    [
        0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x18, 0x53, 0x8B, 0xD9, 0x56, 0x57, 0x8B, 0x83, 0xA8, 0x01, 0x00,
        0x00, 0x8B, 0x73, 0x30, 0x89, 0x45, 0xF4, 0xE8, 0xE4, 0x07, 0xFA, 0xFF,
    ];

    [Fact]
    public void Locate_GameCode_ReturnsTheStartOfTheCollisionHandler()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, CollisionHandlerStart)]);

        // Act
        uint site = OwnerlessTakemiGuardCode.Locate(image);

        // Assert
        Assert.Equal(CodeStart, site);
    }

    [Fact]
    public void Locate_CodeWithoutTheHandler_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => OwnerlessTakemiGuardCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void Code_SiteAndBlock_SkipsObjectsWithoutOwnerAndRunsTheHandlerForTheOthers()
    {
        // Arrange
        uint resumeJumpEnd = Block + OwnerlessTakemiGuardCode.CodeOffset + 29;
        byte[] expected =
        [
            0x83, 0xB9, 0xF4, 0x01, 0x00, 0x00, 0x00,
            0x75, 0x09,
            0xFF, 0x05, .. BitConverter.GetBytes(Block + OwnerlessTakemiGuardCode.SkipsOffset),
            0xC2, 0x04, 0x00,
            0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x18,
            0xE9, .. BitConverter.GetBytes(CodeStart + 6 - resumeJumpEnd),
        ];

        // Act
        byte[] code = OwnerlessTakemiGuardCode.Code(CodeStart, Block);

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void Code_SiteAndBlock_FitsInTheBlock()
    {
        // Arrange
        int end = OwnerlessTakemiGuardCode.CodeOffset;

        // Act
        end += OwnerlessTakemiGuardCode.Code(CodeStart, Block).Length;

        // Assert
        Assert.True(end <= OwnerlessTakemiGuardCode.BlockLength);
    }

    [Fact]
    public void Jump_SiteAndBlock_JumpsToTheCodeOverTheReplacedInstructions()
    {
        // Arrange
        uint distance = Block + OwnerlessTakemiGuardCode.CodeOffset - (CodeStart + 5);

        // Act
        byte[] jump = OwnerlessTakemiGuardCode.Jump(CodeStart, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(distance), 0x90], jump);
    }
}
