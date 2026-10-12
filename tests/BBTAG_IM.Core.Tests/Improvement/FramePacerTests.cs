using BBTAG_IM.Core.Improvement;

namespace BBTAG_IM.Core.Tests.Improvement;

public class FramePacerTests
{
    private const long TicksPerSecond = 600;
    private const int FramesPerSecond = 60;
    private const long Frame = TicksPerSecond / FramesPerSecond;
    private const long Start = 1000;

    public FramePacer CreateFramePacer() => new(TicksPerSecond, FramesPerSecond);

    [Fact]
    public void TicksToWait_FirstFrame_DoesNotWait()
    {
        // Arrange
        FramePacer pacer = CreateFramePacer();

        // Act
        long wait = pacer.TicksToWait(Start);

        // Assert
        Assert.Equal(0, wait);
    }

    [Fact]
    public void TicksToWait_FrameDoneEarly_WaitsForTheRestOfTheFrame()
    {
        // Arrange
        FramePacer pacer = CreateFramePacer();
        pacer.TicksToWait(Start);

        // Act
        long wait = pacer.TicksToWait(Start + 4);

        // Assert
        Assert.Equal(Frame - 4, wait);
    }

    [Fact]
    public void TicksToWait_FramesDoneEarly_KeepsTheDeadlinesOnTheFrameGrid()
    {
        // Arrange
        FramePacer pacer = CreateFramePacer();
        pacer.TicksToWait(Start);
        pacer.TicksToWait(Start + 4);

        // Act
        long wait = pacer.TicksToWait(Start + Frame + 1);

        // Assert
        Assert.Equal(Frame - 1, wait);
    }

    [Fact]
    public void TicksToWait_FrameSlightlyLate_DoesNotWaitAndKeepsTheGrid()
    {
        // Arrange
        FramePacer pacer = CreateFramePacer();
        pacer.TicksToWait(Start);
        pacer.TicksToWait(Start + Frame + 3);

        // Act
        long wait = pacer.TicksToWait(Start + Frame + 5);

        // Assert
        Assert.Equal(Frame - 5, wait);
    }

    [Fact]
    public void TicksToWait_GameStalledForSeveralFrames_StartsANewGridInsteadOfRacing()
    {
        // Arrange
        FramePacer pacer = CreateFramePacer();
        pacer.TicksToWait(Start);
        long stalledUntil = Start + Frame * 10;
        pacer.TicksToWait(stalledUntil);

        // Act
        long wait = pacer.TicksToWait(stalledUntil + 2);

        // Assert
        Assert.Equal(Frame - 2, wait);
    }

    [Fact]
    public void TicksToWait_RateThatDoesNotDivideTheClock_DoesNotDrift()
    {
        // Arrange
        FramePacer pacer = new(1000, 60);
        pacer.TicksToWait(0);

        // Act
        long[] waits = Enumerable.Range(0, 60).Select(_ => pacer.TicksToWait(0)).ToArray();

        // Assert
        Assert.Equal(1000, waits[^1]);
    }

    [Fact]
    public void Constructor_ZeroFramesPerSecond_ThrowsArgumentOutOfRange()
    {
        // Act
        Exception? exception = Record.Exception(() => new FramePacer(TicksPerSecond, 0));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }
}
