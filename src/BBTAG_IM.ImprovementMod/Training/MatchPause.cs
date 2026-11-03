using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Training;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Menu;

namespace BBTAG_IM.ImprovementMod.Training;

internal sealed class MatchPause(GameView game, PauseGate gate, ILog log)
{
    private readonly FrameStepper _stepper = new();
    private readonly HeldStepRepeat _heldStep = new();

    private bool _buttonHeld;

    private bool _reportedPause;

    public bool CanPause => game.InTrainingMatch;

    public bool IsPaused => _stepper.IsPaused;

    public void Toggle() => _stepper.Toggle();

    public void Step() => _stepper.Step();

    public void PauseNow()
    {
        if (IsPaused || !CanPause)
            return;

        Toggle();
        gate.Holds = true;
    }

    public void HoldingNextFrameButton() => _buttonHeld = true;

    public void OnPresent(nint device)
    {
        TakeHotkeys();
        gate.Holds = _stepper.Hold(game.InTrainingMatch);
        Report(gate.TakeCalls());
    }

    private void TakeHotkeys()
    {
        if (MenuInput.TakePresses(Hotkey.Pause) % 2 == 1)
            Toggle();

        for (var press = MenuInput.TakePresses(Hotkey.NextFrame); press > 0; press--)
            Step();

        var held = _buttonHeld || MenuInput.IsHeld(Hotkey.NextFrame);
        _buttonHeld = false;

        if (_heldStep.Steps(IsPaused && held))
            Step();
    }

    private void Report(int checksLastFrame)
    {
        if (IsPaused == _reportedPause)
            return;

        _reportedPause = IsPaused;
        SafeLog.Info(log, IsPaused
            ? $"Match paused. The game checked for a pause {checksLastFrame} times in the last frame."
            : "Match running again.");
    }
}
