namespace BBTAG_IM.Core.Meter;

public sealed record MatchFrame(IReadOnlyList<CharacterFrame?> Characters, IReadOnlyList<bool> Projectiles)
{
    public const int Slots = 4;
}
