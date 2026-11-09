namespace BBTAG_IM.Core.Meter;

public sealed class CharacterTrack(int capacity)
{
    public const int DefaultCapacity = 600;

    private const int NotYet = -1;

    private readonly List<MeterCell> _cells = new(capacity);

    private long _next;
    private Move? _move;
    private Contact _previousContact;
    private bool _previousProjectile;

    public IReadOnlyList<MeterCell> Cells => _cells;

    public MoveTiming? LastMove { get; private set; }

    public void Add(CharacterFrame frame, bool projectileActive)
    {
        var kind = Classify(frame, projectileActive);
        Append(new MeterCell(kind, frame.Invulnerability, MarksOf(frame, projectileActive)));
        _previousContact = frame.Contact;
        _previousProjectile = projectileActive;
    }

    public void AddEmpty()
    {
        Finish();
        Append(MeterCell.Empty);
        _previousContact = Contact.None;
        _previousProjectile = false;
    }

    public void Clear()
    {
        _cells.Clear();
        _move = null;
    }

    private FrameKind Classify(CharacterFrame frame, bool projectileActive)
    {
        var group = StateNames.GroupOf(frame.State);
        var outside = OutsideAMove(frame, group);

        if (outside is { } kind)
        {
            Finish();
            return kind;
        }

        return Act(frame, projectileActive);
    }

    private static FrameKind? OutsideAMove(CharacterFrame frame, StateGroup group)
    {
        if (group == StateGroup.Away)
            return FrameKind.Empty;

        if (group == StateGroup.Leaving)
            return FrameKind.Leaving;

        if (frame.Hitstun > 0 || group == StateGroup.Damage)
            return FrameKind.Hitstun;

        if (group == StateGroup.Tech)
            return FrameKind.Tech;

        if (frame.Blockstun > 0 || (group == StateGroup.Guard && !frame.CanAct))
            return FrameKind.Blockstun;

        if (group == StateGroup.Movement && frame.Hitboxes == 0)
            return FrameKind.Movement;

        return frame.CanAct ? FrameKind.Idle : null;
    }

    private FrameKind Act(CharacterFrame frame, bool projectileActive)
    {
        var move = _move ??= new Move(frame.State, _next, LastMove);
        move.Follow(frame.State, _next);

        if (frame.Hitboxes > 0)
            return Strike(move);

        if (projectileActive && !_previousProjectile)
            return Launch(move);

        Report(move);
        return move.FirstActive == NotYet ? FrameKind.Startup : FrameKind.Recovery;
    }

    private FrameKind Launch(Move move)
    {
        if (move.CancelledAt is { } cancel)
            StartAgain(move, cancel);

        if (_move!.FirstActive == NotYet)
            _move.ProjectileAt(_next);

        Report(_move);
        return FrameKind.Recovery;
    }

    private FrameKind Strike(Move move)
    {
        if (move.CancelledAt is { } cancel)
            StartAgain(move, cancel);

        _move!.ActiveAt(_next);
        Report(_move);
        return FrameKind.Active;
    }

    private void StartAgain(Move cancelled, long cancel)
    {
        LastMove = cancelled.Timing(cancel);
        Relabel(cancel, _next, FrameKind.Startup);
        _move = new Move(cancelled.LatestState, cancel, LastMove);
    }

    private void Finish()
    {
        if (_move is { } move)
            LastMove = move.FirstActive == NotYet ? move.Before : move.Timing(_next);

        _move = null;
    }

    private void Report(Move move) => LastMove = move.Timing(_next + 1);

    private CellMarks MarksOf(CharacterFrame frame, bool projectileActive)
    {
        var marks = CellMarks.None;

        if (projectileActive)
            marks |= CellMarks.Projectile;

        if (frame.Armor)
            marks |= CellMarks.Armor;

        if (frame.Contact != Contact.None)
            marks |= CellMarks.Connected;

        if (frame.Contact != Contact.None && _previousContact == Contact.None)
            marks |= CellMarks.HitStart;

        return marks;
    }

    private void Append(MeterCell cell)
    {
        if (_cells.Count == capacity)
            _cells.RemoveAt(0);

        _cells.Add(cell);
        _next++;
    }

    private void Relabel(long from, long until, FrameKind kind)
    {
        var first = _next - _cells.Count;

        for (var column = Math.Max(from, first); column < until; column++)
        {
            var index = (int)(column - first);
            _cells[index] = _cells[index] with { Kind = kind };
        }
    }

    private sealed class Move(string state, long start, MoveTiming? before)
    {
        public MoveTiming? Before { get; } = before;

        private long _lastActive = NotYet;

        public long Start { get; } = start;

        public long FirstActive { get; private set; } = NotYet;

        public string LatestState { get; private set; } = state;

        public long? CancelledAt { get; private set; }

        private string State { get; set; } = state;

        public void Follow(string state, long column)
        {
            if (state == LatestState)
                return;

            LatestState = state;

            if (FirstActive != NotYet)
                CancelledAt ??= column;
        }

        public void ActiveAt(long column)
        {
            if (FirstActive == NotYet)
            {
                FirstActive = column;
                State = LatestState;
            }

            _lastActive = column;
        }

        public void ProjectileAt(long column)
        {
            FirstActive = column;
            _lastActive = column;
            State = LatestState;
        }

        public MoveTiming Timing(long end)
        {
            var total = (int)(end - Start);

            if (FirstActive == NotYet)
                return new MoveTiming(State, 0, 0, 0, total);

            return new MoveTiming(
                State,
                (int)(FirstActive - Start + 1),
                (int)(_lastActive - FirstActive + 1),
                (int)(end - _lastActive - 1),
                total);
        }
    }
}
