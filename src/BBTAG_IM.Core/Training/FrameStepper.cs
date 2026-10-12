namespace BBTAG_IM.Core.Training;

public sealed class FrameStepper
{
    private int _stepsLeft;

    public bool IsPaused { get; private set; }

    public void Toggle()
    {
        IsPaused = !IsPaused;
        _stepsLeft = 0;
    }

    public void Step()
    {
        if (!IsPaused)
            return;

        _stepsLeft++;
    }

    public bool Hold(bool inTrainingMatch)
    {
        if (!inTrainingMatch)
        {
            IsPaused = false;
            _stepsLeft = 0;
            return false;
        }

        if (!IsPaused)
            return false;

        if (_stepsLeft == 0)
            return true;

        _stepsLeft--;
        return false;
    }
}
