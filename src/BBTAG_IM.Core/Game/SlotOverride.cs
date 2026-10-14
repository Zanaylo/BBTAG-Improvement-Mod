namespace BBTAG_IM.Core.Game;

public sealed record SlotOverride(int Character, int Color, SlotPresence Presence = SlotPresence.Unchanged)
{
    public const int Unchanged = -1;

    public static readonly SlotOverride Keep = new(Unchanged, Unchanged);
}
