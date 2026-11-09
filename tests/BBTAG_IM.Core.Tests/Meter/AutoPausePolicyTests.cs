using BBTAG_IM.Core.Meter;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Tests.Meter;

public class AutoPausePolicyTests
{
    [Fact]
    public void ShouldPause_MoveStartsWithThatOptionOn_Pauses()
    {
        // Arrange
        AutoPauseOptions options = new() { OnMoveStart = true };

        // Act
        bool pause = AutoPausePolicy.ShouldPause(new MeterEvent(MeterEventKind.MoveStarted, Side.Player1, 0), options);

        // Assert
        Assert.True(pause);
    }

    [Fact]
    public void ShouldPause_PlayerNotWatched_DoesNotPause()
    {
        // Arrange
        AutoPauseOptions options = new() { OnMoveStart = true, Player1 = true, Player2 = false };

        // Act
        bool pause = AutoPausePolicy.ShouldPause(new MeterEvent(MeterEventKind.MoveStarted, Side.Player2, 0), options);

        // Assert
        Assert.False(pause);
    }

    [Fact]
    public void ShouldPause_FirstHitWithOnHit_Pauses()
    {
        // Arrange
        AutoPauseOptions options = new() { OnHit = true };

        // Act
        bool pause = AutoPausePolicy.ShouldPause(new MeterEvent(MeterEventKind.HitLanded, Side.Player1, 1), options);

        // Assert
        Assert.True(pause);
    }

    [Fact]
    public void ShouldPause_LaterHitWithOnlyOnHit_DoesNotPause()
    {
        // Arrange
        AutoPauseOptions options = new() { OnHit = true };

        // Act
        bool pause = AutoPausePolicy.ShouldPause(new MeterEvent(MeterEventKind.HitLanded, Side.Player1, 2), options);

        // Assert
        Assert.False(pause);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(4, false)]
    public void ShouldPause_ComboReachesAStop_PausesOnlyOnTheStops(int hits, bool expected)
    {
        // Arrange
        AutoPauseOptions options = new() { OnCombo = true, ComboStops = [3, 5] };

        // Act
        bool pause = AutoPausePolicy.ShouldPause(new MeterEvent(MeterEventKind.HitLanded, Side.Player1, hits), options);

        // Assert
        Assert.Equal(expected, pause);
    }

    [Fact]
    public void ShouldPause_BlockedHitsReachAStop_Pauses()
    {
        // Arrange
        AutoPauseOptions options = new() { OnBlock = true, BlockStops = [2] };

        // Act
        bool pause = AutoPausePolicy.ShouldPause(new MeterEvent(MeterEventKind.HitBlocked, Side.Player1, 2), options);

        // Assert
        Assert.True(pause);
    }

    [Fact]
    public void ShouldPause_EverythingOff_NeverPauses()
    {
        // Arrange
        AutoPauseOptions options = new();

        // Act
        bool pause = AutoPausePolicy.ShouldPause(new MeterEvent(MeterEventKind.HitLanded, Side.Player1, 3), options);

        // Assert
        Assert.False(pause);
    }
}
