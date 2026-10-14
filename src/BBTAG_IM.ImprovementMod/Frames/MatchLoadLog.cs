using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.ImprovementMod.Menu;

namespace BBTAG_IM.ImprovementMod.Frames;

internal sealed class MatchLoadLog(GameView game, CharacterRoster roster, StageTable stages, ILog log)
{
    private bool _inMatch;

    public void OnPresent(nint device)
    {
        var inMatch = game.State == GameState.Match;

        if (inMatch && !_inMatch)
            SafeLog.Info(log, $"The match loaded on {StageName()}: {string.Join(", ", PlayerSlots.InPlayerOrder.Select(Loaded))}.");

        _inMatch = inMatch;
    }

    private string Loaded(int slot) => $"{PlayerSlots.NameOf(slot)} {(game.ChosenCharacter(slot) is { } character ? NameOf(character) : "unknown")}";

    private string StageName() =>
        game.ChosenStage is { } stage ? $"stage {stage} ({stages.NameOf(stage) ?? "unknown"})" : "an unknown stage";

    private string NameOf(int character) => $"{character} {roster.Identify(character).Name}";
}
