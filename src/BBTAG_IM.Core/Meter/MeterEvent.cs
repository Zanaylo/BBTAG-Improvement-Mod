namespace BBTAG_IM.Core.Meter;

public enum MeterEventKind
{
    MoveStarted,
    HitLanded,
    HitBlocked,
}

public sealed record MeterEvent(MeterEventKind Kind, Side Attacker, int Count);
