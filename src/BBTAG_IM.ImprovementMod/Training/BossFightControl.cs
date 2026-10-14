using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.ImprovementMod.Frames;

namespace BBTAG_IM.ImprovementMod.Training;

internal sealed class BossFightControl(MatchSetupGate gate, CharacterRoster roster, StageTable stages, IReadOnlyList<Boss> bosses, ILog log)
{
    private BossFight? _fight;

    public IReadOnlyList<Boss> Bosses => bosses;

    public Boss? Chosen => _fight?.Boss;

    public string? NotReady { get; private set; }

    public void Choose(Boss? boss)
    {
        TurnOff();

        if (boss is not null)
            TurnOn(boss);
    }

    public void OnPresent(nint device)
    {
        var setups = gate.TakeSetups();

        if (_fight is not { } fight)
            return;

        EnsureSprites(fight);

        if (setups > 0)
            SafeLog.Info(log, $"Player 2 was turned into {fight.Boss.Name} (character {fight.Character}) alone on stage {fight.Stage} ({fight.Boss.Stage}) for this match.");
    }

    private void TurnOn(Boss boss)
    {
        try
        {
            var fight = boss.FindIn(roster, stages);
            EnsureSprites(fight);
            gate.Apply(fight.Setup);
            _fight = fight;
            SafeLog.Info(log, $"{boss.Name} fight on. The next time a match is confirmed on character select, Player 2 becomes {boss.Name} alone on stage {fight.Stage} ({boss.Stage}).");
        }
        catch (BossNotReadyException exception)
        {
            NotReady = exception.Message;
            SafeLog.Info(log, $"{exception.Message} The boss fight stays off.");
        }
    }

    private void TurnOff()
    {
        NotReady = null;

        if (_fight is null)
            return;

        gate.Keep();
        SafeLog.Info(log, $"{_fight.Boss.Name} fight off. Character select keeps what is picked again.");
        _fight = null;
    }

    private void EnsureSprites(BossFight fight)
    {
        if (fight.Boss.Sprites is { } sprites && roster.EnsureSpriteFolder(fight.Character, sprites.Folder))
            SafeLog.Info(log, $"{fight.Boss.Name}'s sprites now load from the {sprites.Directory} folder.");
    }
}
