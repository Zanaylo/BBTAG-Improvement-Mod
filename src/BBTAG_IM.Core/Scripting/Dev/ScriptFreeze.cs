using BBTAG_IM.Core.Training;

namespace BBTAG_IM.Core.Scripting.Dev;

public sealed class ScriptFreeze(IMatchPause pause)
{
    private bool _holding;

    public bool IsFrozen => _holding && pause.IsPaused;

    public void Follow(bool waitsForFix)
    {
        if (waitsForFix && !_holding)
        {
            pause.PauseNow();
            _holding = pause.IsPaused;
            return;
        }

        if (waitsForFix || !_holding)
            return;

        _holding = false;

        if (pause.IsPaused)
            pause.Resume();
    }
}
