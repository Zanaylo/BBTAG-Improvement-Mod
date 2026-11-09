using BBTAG_IM.Core.Meter;

namespace BBTAG_IM.Core.Tests.Meter;

public class MoveNamesTests
{
    [Theory]
    [InlineData("NmlAtk5A", "5A")]
    [InlineData("NmlAtkJ2C", "J2C")]
    [InlineData("CmnActChangePartnerAssistAtk_A", "ASSIST A")]
    [InlineData("CmnActAssistAtkB", "ASSIST B")]
    [InlineData("CmnActInvincibleAttack", "REVERSAL")]
    [InlineData("CmnActInvincibleAttackAir", "AIR REVERSAL")]
    [InlineData("CmnActCrushAttack", "CRUSH")]
    [InlineData("CmnActBurstBegin", "BurstBegin")]
    [InlineData("Yumi_Fubuki", "Yumi_Fubuki")]
    public void Of_StateName_ReturnsAShortReadableName(string state, string expected)
    {
        // Act
        string name = MoveNames.Of(state);

        // Assert
        Assert.Equal(expected, name);
    }
}
