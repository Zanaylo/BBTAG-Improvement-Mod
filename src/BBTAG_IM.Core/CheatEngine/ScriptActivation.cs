namespace BBTAG_IM.Core.CheatEngine;

public enum ActivationState
{
    TurningOn,
    On,
    Stuck,
    TimedOut,
}

public sealed class ScriptActivation
{
    public static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan Limit = TimeSpan.FromMinutes(5);

    private TimeSpan? _quietSince;
    private ActivationState _quietState;

    public ActivationState Observe(BridgeStatus status, TimeSpan elapsed)
    {
        if (status.IsWorking)
        {
            Reset();
            return TurningOnUntilTheLimit(elapsed);
        }

        var quietState = status.Waiting > 0 ? ActivationState.Stuck : ActivationState.On;

        if (_quietSince is null || _quietState != quietState)
        {
            _quietSince = elapsed;
            _quietState = quietState;
        }

        if (elapsed - _quietSince.Value >= QuietPeriod)
            return quietState;

        return TurningOnUntilTheLimit(elapsed);
    }

    public void Reset() => _quietSince = null;

    private static ActivationState TurningOnUntilTheLimit(TimeSpan elapsed) =>
        elapsed >= Limit ? ActivationState.TimedOut : ActivationState.TurningOn;
}
