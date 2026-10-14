using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Game;

public class MatchExitTests
{
    private const uint Game = 0x00E59100;
    private const uint Battle = 0x014A77A0;
    private const uint BattleMode = Battle + 0xA4898;

    private readonly FakeGameMemory _memoryMock = new();

    [Fact]
    public void ToCharacterSelect_InAMatch_AsksTheBattleToGoBackToCharacterSelect()
    {
        // Arrange
        _memoryMock.Map(Game + 0x114, 8).Int32At(Game + 0x118, (int)GameState.Match).Map(BattleMode, 4).Int32At(BattleMode, 3);
        MatchExit exit = CreateMatchExit();

        // Act
        bool requested = exit.ToCharacterSelect();

        // Assert
        Assert.True(requested);
        Assert.Equal(7, _memoryMock.Int32From(BattleMode));
    }

    [Fact]
    public void ToCharacterSelect_OutsideOfAMatch_ChangesNothing()
    {
        // Arrange
        _memoryMock.Map(Game + 0x114, 8).Int32At(Game + 0x118, 4).Map(BattleMode, 4).Int32At(BattleMode, 3);
        MatchExit exit = CreateMatchExit();

        // Act
        bool requested = exit.ToCharacterSelect();

        // Assert
        Assert.False(requested);
        Assert.Equal(3, _memoryMock.Int32From(BattleMode));
    }

    public MatchExit CreateMatchExit() => new(new GameView(_memoryMock, new GameAddresses(Game, Battle, 0, 0)));
}
