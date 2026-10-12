namespace BBTAG_IM.Core.CheatEngine;

public enum ActivationState
{
    TurningOn,
    On,
    TimedOut,
}

public sealed class ScriptActivation
{
    public static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan Limit = TimeSpan.FromMinutes(5);

    private TimeSpan? _idleSince;

    public ActivationState Observe(bool busy, TimeSpan elapsed)
    {
        _idleSince = busy ? null : _idleSince ?? elapsed;

        if (_idleSince is not null && elapsed - _idleSince.Value >= QuietPeriod)
            return ActivationState.On;

        return elapsed >= Limit ? ActivationState.TimedOut : ActivationState.TurningOn;
    }
}
