namespace BBTAG_IM.Core.Meter;

public static class StateNames
{
    private const string CommonPrefix = "CmnAct";
    private const string GuardWord = "Guard";
    private const string GuardBreakPrefix = "GuardBreak";
    private const string PartnerPrefix = "ChangePartner";

    private static readonly (string Prefix, StateGroup Group)[] Prefixes =
    [
        ("TagBattleWait", StateGroup.Away),
        ("ChangeReturn", StateGroup.Leaving),
        ("ChangePartnerOut", StateGroup.Leaving),
        ("ChangePartnerQuickOut", StateGroup.Leaving),
        (PartnerPrefix, StateGroup.Partner),
        ("AssistAtk", StateGroup.Partner),
        ("TagOverDriveAssist", StateGroup.Partner),
        ("Ukemi", StateGroup.Tech),
        ("Hit", StateGroup.Damage),
        ("FDown", StateGroup.Damage),
        ("BDown", StateGroup.Damage),
        ("VDown", StateGroup.Damage),
        ("Blowoff", StateGroup.Damage),
        ("WallBound", StateGroup.Damage),
        ("Stagger", StateGroup.Damage),
        (GuardBreakPrefix, StateGroup.Damage),
        ("LandSpin", StateGroup.Damage),
        ("VertSpin", StateGroup.Damage),
        ("KirimomiUpper", StateGroup.Damage),
        ("Aomuke", StateGroup.Damage),
        ("Slide", StateGroup.Damage),
        ("Skeleton", StateGroup.Damage),
        ("LockWait", StateGroup.Damage),
        ("LockReject", StateGroup.Damage),
        ("AirLockWait", StateGroup.Damage),
        ("AirLockReject", StateGroup.Damage),
        ("Freeze", StateGroup.Damage),
        ("FWalk", StateGroup.Movement),
        ("BWalk", StateGroup.Movement),
        ("FDash", StateGroup.Movement),
        ("BDash", StateGroup.Movement),
        ("AirFDash", StateGroup.Movement),
        ("AirBDash", StateGroup.Movement),
        ("StandTurn", StateGroup.Movement),
        ("CrouchTurn", StateGroup.Movement),
        ("AirTurn", StateGroup.Movement),
        ("JumpPre", StateGroup.Movement),
        ("JumpUpper", StateGroup.Movement),
        ("JumpDown", StateGroup.Movement),
        ("JumpLanding", StateGroup.Movement),
        ("Stand", StateGroup.Neutral),
        ("Crouch", StateGroup.Neutral),
    ];

    public static StateGroup GroupOf(string state)
    {
        if (!state.StartsWith(CommonPrefix, StringComparison.Ordinal))
            return StateGroup.Action;

        var name = state.AsSpan(CommonPrefix.Length);

        if (IsGuard(name))
            return StateGroup.Guard;

        foreach (var (prefix, group) in Prefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return group;
        }

        return StateGroup.Action;
    }

    private static bool IsGuard(ReadOnlySpan<char> name) =>
        name.Contains(GuardWord, StringComparison.Ordinal)
        && !name.StartsWith(GuardBreakPrefix, StringComparison.Ordinal)
        && !name.StartsWith(PartnerPrefix, StringComparison.Ordinal);
}
