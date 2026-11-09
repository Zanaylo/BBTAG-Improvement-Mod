namespace BBTAG_IM.Core.Meter;

public sealed class FrameMeterRecorder
{
    public const int QuietFramesBeforeRestart = 20;

    private const int Teams = 2;
    private const int Members = 2;

    private readonly int _capacity;
    private readonly CharacterTrack[] _tracks;
    private readonly List<byte>[] _points = [[], []];
    private readonly byte[] _currentPoint = new byte[Teams];
    private readonly TeamProgress[] _teams = [new(), new()];
    private readonly CharacterFrame?[] _previous = new CharacterFrame?[MatchFrame.Slots];
    private readonly int[] _pendingLoss = new int[Teams];

    private int? _lastGameFrame;
    private long _tick;
    private bool _interacting;
    private bool _stunSeen;
    private long _exchangeStart;
    private int? _advantage;

    public FrameMeterRecorder(int capacity)
    {
        _capacity = capacity;
        _tracks = [.. Enumerable.Range(0, MatchFrame.Slots).Select(_ => new CharacterTrack(capacity))];
    }

    public event Action<MeterEvent>? Happened;

    public int Columns => _tracks[0].Cells.Count;

    public int Freeze { get; private set; }

    public int QuietRun { get; private set; }

    public void Record(MatchFrame match)
    {
        if (match.Characters.FirstOrDefault(character => character is not null) is not { } any || any.Frame == _lastGameFrame)
            return;

        _lastGameFrame = any.Frame;
        DetectContacts(match);

        if (!Progressed(match))
        {
            Freeze++;
            Remember(match);
            return;
        }

        if (AddsAColumn(match))
            Append(match);

        Analyze(match);
        Remember(match);
    }

    public void Reset()
    {
        ClearColumns();
        _lastGameFrame = null;
        _tick = 0;
        _interacting = false;
        _stunSeen = false;
        _advantage = null;
        _exchangeStart = 0;
        Array.Clear(_previous);
        Array.Clear(_currentPoint);
        Array.Clear(_pendingLoss);

        foreach (var team in _teams)
            team.Clear();
    }

    public IReadOnlyList<MeterCell> Lane(Side side, Role role)
    {
        var team = (int)side;
        return new LaneView(column => _tracks[SlotOf(team, MemberAt(team, column, role))].Cells[column], Columns);
    }

    public int PointOf(Side side) => _currentPoint[(int)side];

    public MoveTiming? LastMove(Side side, Role role)
    {
        var team = (int)side;
        var member = role == Role.Point ? _currentPoint[team] : 1 - _currentPoint[team];
        return _tracks[SlotOf(team, member)].LastMove;
    }

    public MeterView View(IReadOnlyList<string> names) => new(SideOf(Side.Player1, names), SideOf(Side.Player2, names), Freeze);

    public int? Advantage(Side side) => side == Side.Player1 ? _advantage : -_advantage;

    public int? Gap(Side side) => _teams[(int)side].Gap;

    public ComboReadout Combo(Side side) => _teams[(int)side].Combo;

    private SideView SideOf(Side side, IReadOnlyList<string> names)
    {
        var team = (int)side;
        var point = _currentPoint[team];

        return new SideView(
            names[SlotOf(team, point)],
            names[SlotOf(team, 1 - point)],
            Lane(side, Role.Point),
            Lane(side, Role.Partner),
            LastMove(side, Role.Point),
            LastMove(side, Role.Partner),
            Advantage(side),
            Gap(side),
            Combo(side));
    }

    private bool AddsAColumn(MatchFrame match)
    {
        if (IsQuiet(match))
        {
            QuietRun++;
            return QuietRun <= QuietFramesBeforeRestart;
        }

        if (QuietRun > QuietFramesBeforeRestart)
            ClearColumns();

        QuietRun = 0;
        return true;
    }

    private void Append(MatchFrame match)
    {
        for (var slot = 0; slot < MatchFrame.Slots; slot++)
            AddTo(slot, match.Characters[slot], match.Projectiles[slot]);

        for (var team = 0; team < Teams; team++)
        {
            _currentPoint[team] = PointAfter(team, match);
            Keep(_points[team], _currentPoint[team]);
        }

        RaiseMoveStarts();
    }

    private void AddTo(int slot, CharacterFrame? frame, bool projectile)
    {
        if (frame is null)
        {
            _tracks[slot].AddEmpty();
            return;
        }

        _tracks[slot].Add(frame, projectile);
    }

    private void Keep(List<byte> points, byte point)
    {
        if (points.Count == _capacity)
            points.RemoveAt(0);

        points.Add(point);
    }

    private void RaiseMoveStarts()
    {
        for (var team = 0; team < Teams; team++)
        {
            if (StartedAMove(_tracks[SlotOf(team, _currentPoint[team])].Cells))
                Happened?.Invoke(new MeterEvent(MeterEventKind.MoveStarted, (Side)team, 0));
        }
    }

    private static bool StartedAMove(IReadOnlyList<MeterCell> cells)
    {
        if (cells.Count == 0 || cells[^1].Kind != FrameKind.Startup)
            return false;

        return cells.Count == 1 || cells[^2].Kind is not (FrameKind.Startup or FrameKind.Active or FrameKind.Recovery);
    }

    private void Analyze(MatchFrame match)
    {
        _tick++;

        for (var team = 0; team < Teams; team++)
        {
            _teams[team].Observe(_tick, match.Characters[SlotOf(team, _currentPoint[team])], _pendingLoss[team]);
            _pendingLoss[team] = 0;
        }

        MeasureAdvantage();
    }

    private void DetectContacts(MatchFrame match)
    {
        for (var team = 0; team < Teams; team++)
        {
            var slot = SlotOf(team, _currentPoint[team]);
            var progress = _teams[team];
            _pendingLoss[team] += HealthLost(match, team);

            if (progress.NewContact(match.Characters[slot], _previous[slot]) is { } contact)
                Happened?.Invoke(new MeterEvent(contact, (Side)(1 - team), progress.CountOf(contact)));
        }
    }

    private void MeasureAdvantage()
    {
        var bothFree = _teams.All(team => team.IsFree);

        if (!bothFree)
        {
            if (!_interacting)
                _exchangeStart = _tick;

            _stunSeen = (_interacting && _stunSeen) || _teams.Any(team => team.IsStunned);
            _interacting = true;
            return;
        }

        if (!_interacting)
            return;

        _interacting = false;

        if (_stunSeen)
            _advantage = (int)(Math.Max(_teams[1].FreeSince, _exchangeStart) - Math.Max(_teams[0].FreeSince, _exchangeStart));
    }

    private static bool IsQuiet(MatchFrame match)
    {
        for (var slot = 0; slot < MatchFrame.Slots; slot++)
        {
            if (match.Projectiles[slot])
                return false;

            if (match.Characters[slot] is { } frame && !IsCalm(frame))
                return false;
        }

        return true;
    }

    private static bool IsCalm(CharacterFrame frame)
    {
        var group = StateNames.GroupOf(frame.State);

        if (group is StateGroup.Away or StateGroup.Leaving)
            return true;

        return frame.CanAct && frame.Hitstun == 0 && frame.Blockstun == 0 && group is StateGroup.Neutral or StateGroup.Movement or StateGroup.Guard;
    }

    private bool Progressed(MatchFrame match)
    {
        var onScreen = false;

        for (var slot = 0; slot < MatchFrame.Slots; slot++)
        {
            if (match.Characters[slot] is not { } frame || StateNames.GroupOf(frame.State) == StateGroup.Away)
                continue;

            onScreen = true;

            if (frame.Hitstop == 0 && Moved(_previous[slot], frame))
                return true;
        }

        return !onScreen;
    }

    private static bool Moved(CharacterFrame? before, CharacterFrame now) =>
        before is null || before.State != now.State || before.StateTime != now.StateTime;

    private byte PointAfter(int team, MatchFrame match)
    {
        var first = CanBePoint(match.Characters[SlotOf(team, 0)]);
        var second = CanBePoint(match.Characters[SlotOf(team, 1)]);

        if (first == second)
            return _currentPoint[team];

        return first ? (byte)0 : (byte)1;
    }

    private static bool CanBePoint(CharacterFrame? frame) =>
        frame is not null && StateNames.GroupOf(frame.State) is not (StateGroup.Away or StateGroup.Partner or StateGroup.Leaving);

    private void ClearColumns()
    {
        foreach (var track in _tracks)
            track.Clear();

        foreach (var points in _points)
            points.Clear();

        Freeze = 0;
        QuietRun = 0;
    }

    private void Remember(MatchFrame match)
    {
        for (var slot = 0; slot < MatchFrame.Slots; slot++)
            _previous[slot] = match.Characters[slot];
    }

    private int MemberAt(int team, int column, Role role)
    {
        var point = column < _points[team].Count ? _points[team][column] : _currentPoint[team];
        return role == Role.Point ? point : 1 - point;
    }

    private int HealthLost(MatchFrame match, int team)
    {
        var lost = 0;

        for (var member = 0; member < Members; member++)
        {
            var slot = SlotOf(team, member);

            if (OnScreen(_previous[slot]) && OnScreen(match.Characters[slot]))
                lost += Math.Max(0, _previous[slot]!.Health - match.Characters[slot]!.Health);
        }

        return lost;
    }

    private static bool OnScreen(CharacterFrame? frame) => frame is not null && StateNames.GroupOf(frame.State) != StateGroup.Away;

    private static int SlotOf(int team, int member) => team + member * Teams;
}
