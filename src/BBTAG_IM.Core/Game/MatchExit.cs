using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public sealed class MatchExit(GameView game)
{
    private const uint BattleModeOffset = 0xA4898;
    private const int BackToCharacterSelect = 7;

    public bool ToCharacterSelect()
    {
        if (game.State != GameState.Match || game.BattleAddress is not { } battle)
            return false;

        game.Memory.WriteInt32(battle + BattleModeOffset, BackToCharacterSelect);
        return true;
    }
}
