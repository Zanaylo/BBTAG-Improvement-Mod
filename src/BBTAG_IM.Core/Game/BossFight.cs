namespace BBTAG_IM.Core.Game;

public sealed record BossFight(Boss Boss, int Character, int Stage)
{
    private const int PointColor = 0;
    private const int SoloPartnerColor = 98;

    public MatchSetup Setup
    {
        get
        {
            var slots = MatchSetup.Keep.Slots.ToArray();
            slots[MatchSlots.PlayerTwo] = new SlotOverride(Character, PointColor, SlotPresence.Plays);
            slots[MatchSlots.PlayerTwoPartner] = new SlotOverride(Character, SoloPartnerColor, SlotPresence.SitsOut);
            return new MatchSetup(slots, Stage);
        }
    }
}
