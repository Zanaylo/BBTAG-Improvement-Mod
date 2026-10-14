using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Tests.Game;

public class BossFightTests
{
    private const int Take = 53;
    private const int TakeStage = 119;

    public BossFight CreateBossFight() => new(Bosses.TakeMikazuchi, Take, TakeStage);

    [Fact]
    public void Setup_Fight_MakesPlayerTwoTheBossWithTheFirstColor()
    {
        // Arrange
        BossFight fight = CreateBossFight();

        // Act
        MatchSetup setup = fight.Setup;

        // Assert
        Assert.Equal(new SlotOverride(Take, 0, SlotPresence.Plays), setup.Slots[MatchSlots.PlayerTwo]);
    }

    [Fact]
    public void Setup_Fight_GivesThePartnerTheColorThatMakesTheTeamSolo()
    {
        // Arrange
        BossFight fight = CreateBossFight();

        // Act
        MatchSetup setup = fight.Setup;

        // Assert
        Assert.Equal(98, setup.Slots[MatchSlots.PlayerTwoPartner].Color);
    }

    [Fact]
    public void Setup_Fight_KeepsThePartnerOutOfTheMatch()
    {
        // Arrange
        BossFight fight = CreateBossFight();

        // Act
        MatchSetup setup = fight.Setup;

        // Assert
        Assert.Equal(new SlotOverride(Take, 98, SlotPresence.SitsOut), setup.Slots[MatchSlots.PlayerTwoPartner]);
    }

    [Theory]
    [InlineData(MatchSlots.PlayerOne)]
    [InlineData(MatchSlots.PlayerOnePartner)]
    public void Setup_Fight_KeepsWhatPlayerOnePicked(int slot)
    {
        // Arrange
        BossFight fight = CreateBossFight();

        // Act
        MatchSetup setup = fight.Setup;

        // Assert
        Assert.Equal(SlotOverride.Keep, setup.Slots[slot]);
    }

    [Fact]
    public void Setup_Fight_CoversEverySlot()
    {
        // Arrange
        BossFight fight = CreateBossFight();

        // Act
        MatchSetup setup = fight.Setup;

        // Assert
        Assert.Equal(MatchSlots.Count, setup.Slots.Count);
    }

    [Fact]
    public void Setup_Fight_PlaysTheMatchOnTheBossStage()
    {
        // Arrange
        BossFight fight = CreateBossFight();

        // Act
        MatchSetup setup = fight.Setup;

        // Assert
        Assert.Equal(TakeStage, setup.Stage);
    }

    [Fact]
    public void Keep_NoFight_KeepsEverySlotAndTheStage()
    {
        // Act
        MatchSetup setup = MatchSetup.Keep;

        // Assert
        Assert.All(setup.Slots, slot => Assert.Equal(SlotOverride.Keep, slot));
        Assert.Equal(MatchSlots.Count, setup.Slots.Count);
        Assert.Equal(MatchSetup.UnchangedStage, setup.Stage);
    }
}
