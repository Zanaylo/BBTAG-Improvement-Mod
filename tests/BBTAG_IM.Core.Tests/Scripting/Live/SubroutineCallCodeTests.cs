using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting.Live;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class SubroutineCallCodeTests
{
    private const uint CodeStart = 0x007693F0;

    private static readonly byte[] CallSubroutine =
    [
        0x55, 0x8B, 0xEC, 0x6A, 0xFF, 0x68, 0x48, 0x45, 0xA5, 0x00, 0x64, 0xA1, 0x00, 0x00, 0x00, 0x00,
        0x50, 0x83, 0xEC, 0x34, 0xA1, 0x80, 0xD3, 0xBF, 0x00, 0x33, 0xC5, 0x89, 0x45, 0xF0, 0x53, 0x56,
        0x57, 0x50, 0x8D, 0x45, 0xF4, 0x64, 0xA3, 0x00, 0x00, 0x00, 0x00, 0x8B, 0xD9, 0x8B, 0x45, 0x08,
        0x33, 0xC9, 0x8B, 0x93, 0x00, 0x14, 0x00, 0x00,
    ];

    [Fact]
    public void Locate_GameCode_ReturnsTheStartOfTheFunction()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, CallSubroutine)]);

        // Act
        uint callSubroutine = SubroutineCallCode.Locate(image);

        // Assert
        Assert.Equal(CodeStart, callSubroutine);
    }

    [Fact]
    public void Locate_CodeWithoutTheFunction_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => SubroutineCallCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }
}
