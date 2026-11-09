using BBTAG_IM.Core.Meter;

namespace BBTAG_IM.Core.Tests.Meter;

public class StateNamesTests
{
    [Theory]
    [InlineData("CmnActHitStandLv4", StateGroup.Damage)]
    [InlineData("CmnActHitCrouchLv2", StateGroup.Damage)]
    [InlineData("CmnActFDownLoop", StateGroup.Damage)]
    [InlineData("CmnActBDown2Stand", StateGroup.Damage)]
    [InlineData("CmnActVDownUpper", StateGroup.Damage)]
    [InlineData("CmnActBlowoff", StateGroup.Damage)]
    [InlineData("CmnActWallBound", StateGroup.Damage)]
    [InlineData("CmnActStaggerLoop", StateGroup.Damage)]
    [InlineData("CmnActGuardBreakStand", StateGroup.Damage)]
    [InlineData("CmnActLandSpin", StateGroup.Damage)]
    [InlineData("CmnActSlideKeep", StateGroup.Damage)]
    [InlineData("CmnActUkemiLandN", StateGroup.Tech)]
    [InlineData("CmnActUkemiAirF", StateGroup.Tech)]
    [InlineData("CmnActHighHeavyGuardLoop", StateGroup.Guard)]
    [InlineData("CmnActCrouchGuardPre", StateGroup.Guard)]
    [InlineData("CmnActAirGuardEnd", StateGroup.Guard)]
    [InlineData("CmnActFWalk", StateGroup.Movement)]
    [InlineData("CmnActBDash", StateGroup.Movement)]
    [InlineData("CmnActAirFDash", StateGroup.Movement)]
    [InlineData("CmnActFDashStop", StateGroup.Movement)]
    [InlineData("CmnActStandTurn", StateGroup.Movement)]
    [InlineData("CmnActJumpPre", StateGroup.Movement)]
    [InlineData("CmnActJumpUpper", StateGroup.Movement)]
    [InlineData("CmnActJumpLanding", StateGroup.Movement)]
    [InlineData("CmnActStand", StateGroup.Neutral)]
    [InlineData("CmnActCrouch", StateGroup.Neutral)]
    [InlineData("CmnActStand2Crouch", StateGroup.Neutral)]
    [InlineData("CmnActTagBattleWait", StateGroup.Away)]
    [InlineData("CmnActChangePartnerAssistAtk_A", StateGroup.Partner)]
    [InlineData("CmnActChangePartnerAssistAdmission", StateGroup.Partner)]
    [InlineData("CmnActAssistAtkB", StateGroup.Partner)]
    [InlineData("CmnActTagOverDriveAssist", StateGroup.Partner)]
    [InlineData("CmnActChangeReturnAppeal", StateGroup.Leaving)]
    [InlineData("CmnActChangePartnerOut", StateGroup.Leaving)]
    [InlineData("CmnActChangePartnerQuickOut", StateGroup.Leaving)]
    [InlineData("NmlAtk5A", StateGroup.Action)]
    [InlineData("CmnActInvincibleAttack", StateGroup.Action)]
    [InlineData("CmnActLandingStiffLoop", StateGroup.Action)]
    [InlineData("", StateGroup.Action)]
    public void GroupOf_StateName_ReturnsItsGroup(string state, StateGroup expected)
    {
        // Act
        StateGroup group = StateNames.GroupOf(state);

        // Assert
        Assert.Equal(expected, group);
    }
}
