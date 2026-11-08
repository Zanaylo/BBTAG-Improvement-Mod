namespace BBTAG_IM.Core.Hantei;

public sealed record Ha6Attack(int Damage, int Proration, int GuardFlags, int HitFlags, int GroundHitVector, int AirHitVector, int UntechTime, int GroundGuardVector = 0)
{
    private const int StandBlockable = 1 << 0;
    private const int CrouchBlockable = 1 << 2;
    private const int NoGroundTech = 1 << 8;
    private const int VectorHeaderLength = 2;
    private const int VectorRowLength = 4;
    private const int HitVectorField = 1;
    private const int GuardVectorField = 3;
    private const int AirRow = 1;

    public bool IsLow => (GuardFlags & StandBlockable) == 0 && (GuardFlags & CrouchBlockable) != 0;

    public bool IsOverhead => (GuardFlags & StandBlockable) != 0 && (GuardFlags & CrouchBlockable) == 0;

    public bool IsHardKnockdown => (HitFlags & NoGroundTech) != 0;

    public static Ha6Attack? Of(Ha6Frame frame)
    {
        if (frame.Attack.Count == 0)
            return null;

        return new Ha6Attack(
            First(frame, "ATAT"),
            First(frame, "ATC0"),
            First(frame, "ATGD"),
            First(frame, "ATF1"),
            HitVector(frame, 0),
            HitVector(frame, AirRow),
            First(frame, "ATSU"),
            GuardVector(frame));
    }

    private static int First(Ha6Frame frame, string chunk) => frame.Attack.TryGetValue(chunk, out var values) && values.Length > 0 ? values[0] : 0;

    private static int GuardVector(Ha6Frame frame) =>
        frame.Attack.TryGetValue("ATV2", out var values) && VectorHeaderLength + GuardVectorField < values.Length ? values[VectorHeaderLength + GuardVectorField] : 0;

    private static int HitVector(Ha6Frame frame, int row)
    {
        var field = VectorHeaderLength + row * VectorRowLength + HitVectorField;
        return frame.Attack.TryGetValue("ATV2", out var values) && field < values.Length ? values[field] : 0;
    }
}
