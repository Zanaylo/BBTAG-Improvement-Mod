namespace BBTAG_IM.Core.Tests.Support;

internal sealed class ManualClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 12, 20, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan elapsed) => _now += elapsed;
}
