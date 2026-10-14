using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Game;

public class MatchSetupCodeTests
{
    private const uint CodeStart = 0x0084C22B;
    private const uint Site = 0x0084C238;
    private const uint Called = 0x008C2D60;
    private const uint Block = 0x10000000;

    private static readonly byte[] MusicStoreEnd =
    [
        0x3D, 0x00, 0x02, 0x00, 0x00, 0x74, 0x06, 0x89, 0x86, 0x50, 0x28, 0x00, 0x00,
        0xE8, 0x23, 0x6B, 0x07, 0x00, 0x8B, 0xC8,
    ];

    [Fact]
    public void Locate_GameCode_ReturnsTheCallAfterTheMusicIsStored()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, MusicStoreEnd)]);

        // Act
        uint site = MatchSetupCode.Locate(image);

        // Assert
        Assert.Equal(Site, site);
    }

    [Fact]
    public void Locate_CodeWithoutTheMusicStore_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => MatchSetupCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void CalledBy_GameCode_ReturnsTheFunctionTheSiteCalls()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, MusicStoreEnd)]);

        // Act
        uint called = MatchSetupCode.CalledBy(image, Site);

        // Assert
        Assert.Equal(Called, called);
    }

    [Fact]
    public void Code_SiteBlockAndCall_WritesEveryChosenSlotItsPresenceAndTheStageThenCallsTheGameAndReturns()
    {
        // Arrange
        byte[] choose =
        [
            .. Override(Block + 0, 0x27A8),
            .. Override(Block + 16, 0x27B0),
            .. Override(Block + 32, 0x27C4),
            .. Override(Block + 4, 0x27D0),
            .. Override(Block + 20, 0x27D8),
            .. Override(Block + 36, 0x27EC),
            .. Override(Block + 8, 0x27F8),
            .. Override(Block + 24, 0x2800),
            .. Override(Block + 40, 0x2814),
            .. Override(Block + 12, 0x2820),
            .. Override(Block + 28, 0x2828),
            .. Override(Block + 44, 0x283C),
            .. Override(Block + 48, 0x284C),
            0xFF, 0x05, .. BitConverter.GetBytes(Block + 52),
        ];
        uint callEnd = Block + MatchSetupCode.CodeOffset + (uint)choose.Length + 5;
        byte[] call = [0xE8, .. BitConverter.GetBytes(Called - callEnd)];
        byte[] back = [0xE9, .. BitConverter.GetBytes(Site + 5 - (callEnd + 5))];

        // Act
        byte[] code = MatchSetupCode.Code(Site, Block, Called);

        // Assert
        Assert.Equal([.. choose, .. call, .. back], code);
    }

    [Fact]
    public void Code_SiteBlockAndCall_FitsInTheBlock()
    {
        // Arrange
        int end = MatchSetupCode.CodeOffset;

        // Act
        end += MatchSetupCode.Code(Site, Block, Called).Length;

        // Assert
        Assert.True(end <= MatchSetupCode.BlockLength);
    }

    [Fact]
    public void Jump_SiteAndBlock_JumpsToTheCodeOfTheBlock()
    {
        // Arrange
        uint distance = Block + MatchSetupCode.CodeOffset - (Site + 5);

        // Act
        byte[] jump = MatchSetupCode.Jump(Site, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(distance)], jump);
    }

    private static byte[] Override(uint source, uint gameField) =>
    [
        0xA1, .. BitConverter.GetBytes(source),
        0x83, 0xF8, 0xFF,
        0x74, 0x06,
        0x89, 0x86, .. BitConverter.GetBytes(gameField),
    ];
}
