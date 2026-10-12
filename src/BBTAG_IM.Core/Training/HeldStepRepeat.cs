namespace BBTAG_IM.Core.Training;

public sealed class HeldStepRepeat
{
    public const int DelayFrames = 20;
    public const int IntervalFrames = 4;

    private int _heldFrames;

    public bool Steps(bool held)
    {
        if (!held)
        {
            _heldFrames = 0;
            return false;
        }

        _heldFrames++;

        if (_heldFrames <= DelayFrames)
            return false;

        return (_heldFrames - DelayFrames - 1) % IntervalFrames == 0;
    }
}
