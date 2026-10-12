namespace BBTAG_IM.Core.Improvement;

public sealed class FramePacer
{
    private readonly long _ticksPerSecond;
    private readonly int _framesPerSecond;
    private readonly long _ticksPerFrame;

    private long? _origin;
    private long _frame;

    public FramePacer(long ticksPerSecond, int framesPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framesPerSecond);

        _ticksPerSecond = ticksPerSecond;
        _framesPerSecond = framesPerSecond;
        _ticksPerFrame = ticksPerSecond / framesPerSecond;
    }

    public long TicksToWait(long now)
    {
        if (_origin is null)
            return StartGrid(now);

        _frame++;
        var deadline = _origin.Value + _frame * _ticksPerSecond / _framesPerSecond;

        if (now <= deadline)
            return deadline - now;

        return now - deadline >= _ticksPerFrame ? StartGrid(now) : 0;
    }

    private long StartGrid(long now)
    {
        _origin = now;
        _frame = 0;
        return 0;
    }
}
