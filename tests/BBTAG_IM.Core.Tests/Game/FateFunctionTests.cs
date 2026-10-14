using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Game;

public class FateFunctionTests
{
    private const uint CodeAddress = 0x401000;
    private const int CodeSize = 0x200;
    private const uint SiteOffset = 0x20;
    private const uint RosterGetterOffset = 0x100;
    private const uint FateGetterOffset = 0x180;

    [Theory]
    [InlineData("85 C0 74 07 33 C0")]
    [InlineData("90 90 90 90 90 90")]
    public void Locate_CodeAskingTheRosterForAFate_ReturnsTheFateFunction(string afterTheCall)
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeAddress, CreateFateCode(afterTheCall).ToArray())]);

        // Act
        uint function = FateFunction.Locate(image);

        // Assert
        Assert.Equal(CodeAddress + FateGetterOffset, function);
    }

    [Fact]
    public void Locate_CodeWithoutTheFateCall_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeAddress, new GameCode(CodeSize).ToArray())]);

        // Act
        Exception? exception = Record.Exception(() => FateFunction.Locate(image));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Character fate was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    private static GameCode CreateFateCode(string afterTheCall) =>
        new GameCode(CodeSize)
            .Bytes(SiteOffset, $"55 8B EC 56 8B 75 08 56 ?? ?? ?? ?? ?? 8B C8 ?? ?? ?? ?? ?? {afterTheCall} 5E 5D C2 04 00")
            .CallAt(SiteOffset + 8, RosterGetterOffset)
            .CallAt(SiteOffset + 15, FateGetterOffset);
}
