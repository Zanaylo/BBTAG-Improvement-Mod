namespace BBTAG_IM.Core.Porting.Moves;

public enum MoveKind
{
    StandingNormal,
    CrouchingNormal,
    AirNormal,
    StandingSpecial,
    AirSpecial,
    Super,
    Throw,
    Reversal,
}

public sealed record MoveObject(int FrameId, string Code, int X, int Y)
{
    private const string StatePrefix = "Obj_";

    public string State => StatePrefix + Code;
}

public sealed record MoveDefinition(
    string State,
    string Input,
    MoveKind Kind,
    IReadOnlyList<int> Patterns,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Cancels,
    IReadOnlyList<string> Followups)
{
    public IReadOnlyList<MoveObject> Objects { get; init; } = [];

    public IReadOnlyList<int> HitPatterns { get; init; } = [];

    public string? HoldButton { get; init; }

    public string? ThrowResult { get; init; }

    public string? Parent { get; init; }

    public string? MoveName { get; init; }

    public IReadOnlyList<string> ParentMoves { get; init; } = [];

    public string? AliasOf { get; init; }

    public int MeterCost { get; init; }

    public bool Flashes { get; init; }

    public bool ReplacesCommonState { get; init; }

    public bool IsAirborne => Kind is MoveKind.AirNormal or MoveKind.AirSpecial;

    public bool IsNormal => Kind is MoveKind.StandingNormal or MoveKind.CrouchingNormal or MoveKind.AirNormal;

    public bool IsRegistered => !ReplacesCommonState;

    public bool HasBody => AliasOf is null;
}
