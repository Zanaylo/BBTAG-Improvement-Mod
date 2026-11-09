namespace BBTAG_IM.Core.Meter;

internal sealed class TeamProgress
{
    private const int LongestGap = 30;

    private bool _wasFree = true;
    private bool _wasBlocking;
    private long? _blockEndedAt;
    private bool _sequenceOpen;
    private bool _justOpened;
    private int _lossBeforeContact;

    public long FreeSince { get; private set; }

    public int? Gap { get; private set; }

    public ComboReadout Combo { get; private set; } = ComboReadout.None;

    public bool IsFree { get; private set; } = true;

    public bool IsStunned { get; private set; }

    public void Clear()
    {
        _wasFree = true;
        _wasBlocking = false;
        _blockEndedAt = null;
        _sequenceOpen = false;
        _justOpened = false;
        _lossBeforeContact = 0;
        FreeSince = 0;
        Gap = null;
        Combo = ComboReadout.None;
        IsFree = true;
        IsStunned = false;
    }

    public void Observe(long tick, CharacterFrame? point, int healthLost)
    {
        var blocking = point is { } guard && IsBlocking(guard);
        var damaged = point is { } hurt && IsDamaged(hurt);
        IsFree = point is null || (point.CanAct && !blocking && !damaged);
        IsStunned = blocking || damaged;

        if (IsFree && !_wasFree)
            FreeSince = tick;

        MeasureGap(tick, blocking);
        TakeLoss(healthLost, blocking);

        if (IsFree)
            _sequenceOpen = false;

        _wasFree = IsFree;
        _wasBlocking = blocking;
    }

    public MeterEventKind? NewContact(CharacterFrame? point, CharacterFrame? previous)
    {
        if (point is null || point.Hitstop == 0 || (previous?.Hitstop ?? 0) > 0)
            return null;

        if (IsDamaged(point))
            return Count(MeterEventKind.HitLanded);

        return IsBlocking(point) ? Count(MeterEventKind.HitBlocked) : null;
    }

    public int CountOf(MeterEventKind kind) => kind == MeterEventKind.HitLanded ? Combo.Hits : Combo.Blocks;

    private MeterEventKind Count(MeterEventKind kind)
    {
        Open();
        Combo = kind == MeterEventKind.HitLanded ? Combo with { Hits = Combo.Hits + 1 } : Combo with { Blocks = Combo.Blocks + 1 };
        return kind;
    }

    private void Open()
    {
        if (_sequenceOpen)
            return;

        _sequenceOpen = true;
        _justOpened = true;
        Combo = ComboReadout.None;
    }

    private void TakeLoss(int lost, bool blocking)
    {
        if (!_sequenceOpen)
        {
            _lossBeforeContact = lost;
            return;
        }

        if (_justOpened)
        {
            lost += _lossBeforeContact;
            _justOpened = false;
        }

        Combo = blocking ? Combo with { Chip = Combo.Chip + lost } : Combo with { Damage = Combo.Damage + lost };
    }

    private void MeasureGap(long tick, bool blocking)
    {
        if (_wasBlocking && !blocking)
            _blockEndedAt = tick;

        if (!blocking || _wasBlocking || _blockEndedAt is not { } ended)
            return;

        var gap = tick - ended;
        Gap = gap <= LongestGap ? (int)gap : Gap;
        _blockEndedAt = null;
    }

    private static bool IsBlocking(CharacterFrame frame) =>
        frame.Blockstun > 0 || (StateNames.GroupOf(frame.State) == StateGroup.Guard && !frame.CanAct);

    private static bool IsDamaged(CharacterFrame frame) =>
        frame.Hitstun > 0 || StateNames.GroupOf(frame.State) is StateGroup.Damage or StateGroup.Tech;
}
