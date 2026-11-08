using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting.Live;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class StateEntryCodeTests
{
    private const uint CodeStart = 0x00768E80;

    private static readonly byte[] EnterState =
    [
        0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x28, 0xA1, 0x80, 0xD3, 0xBF, 0x00, 0x33, 0xC5, 0x89, 0x45, 0xFC,
        0x8B, 0x45, 0x08, 0x53, 0x56, 0x57, 0x8B, 0xD9, 0x89, 0x45, 0xD8, 0xE8, 0x80, 0x64, 0xFD, 0xFF,
        0x8B, 0x80, 0x08, 0x4B, 0x0A, 0x00, 0x89, 0x43, 0x18,
    ];

    [Fact]
    public void Locate_GameCode_ReturnsTheStartOfTheFunction()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, EnterState)]);

        // Act
        uint enterState = StateEntryCode.Locate(image);

        // Assert
        Assert.Equal(CodeStart, enterState);
    }

    [Fact]
    public void Locate_CodeWithoutTheFunction_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => StateEntryCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }
}
