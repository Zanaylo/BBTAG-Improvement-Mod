using BBTAG_IM.Core.Hantei;

namespace BBTAG_IM.Core.Tests.Hantei;

public class Ha6AttackTests
{
    private static Ha6Frame Frame(int guardFlags, int hitFlags = 0) => new()
    {
        Attack = new Dictionary<string, int[]>
        {
            ["ATAT"] = [480],
            ["ATC0"] = [92, 0, 0],
            ["ATGD"] = [guardFlags],
            ["ATF1"] = [hitFlags],
            ["ATSU"] = [22],
            ["ATV2"] = [3, 2, 0, 1, 0, 151, 0, 127, 0, 171, 0, 51, 0, 161],
        },
    };

    [Fact]
    public void Of_FrameWithoutAttack_ReturnsNothing()
    {
        // Act
        Ha6Attack? attack = Ha6Attack.Of(new Ha6Frame());

        // Assert
        Assert.Null(attack);
    }

    [Fact]
    public void Of_AttackFrame_ReadsItsValues()
    {
        // Act
        Ha6Attack attack = Ha6Attack.Of(Frame(37))!;

        // Assert
        Assert.Equal(480, attack.Damage);
        Assert.Equal(92, attack.Proration);
        Assert.Equal(1, attack.GroundHitVector);
        Assert.Equal(127, attack.AirHitVector);
        Assert.Equal(22, attack.UntechTime);
    }

    [Theory]
    [InlineData(37, false, false)]
    [InlineData(36, true, false)]
    [InlineData(3, false, true)]
    public void Height_FollowsTheGuardFlags(int guardFlags, bool low, bool overhead)
    {
        // Act
        Ha6Attack attack = Ha6Attack.Of(Frame(guardFlags))!;

        // Assert
        Assert.Equal(low, attack.IsLow);
        Assert.Equal(overhead, attack.IsOverhead);
    }

    [Fact]
    public void IsHardKnockdown_ReadsTheNoGroundTechFlag()
    {
        // Act
        Ha6Attack attack = Ha6Attack.Of(Frame(37, 0x100))!;

        // Assert
        Assert.True(attack.IsHardKnockdown);
    }
}
