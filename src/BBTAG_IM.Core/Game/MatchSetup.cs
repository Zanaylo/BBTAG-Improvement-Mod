namespace BBTAG_IM.Core.Game;

public sealed record MatchSetup(IReadOnlyList<SlotOverride> Slots, int Stage)
{
    public const int UnchangedStage = -1;

    public static readonly MatchSetup Keep = new(Enumerable.Repeat(SlotOverride.Keep, MatchSlots.Count).ToArray(), UnchangedStage);
}
