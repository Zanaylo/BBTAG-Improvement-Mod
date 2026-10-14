using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Launch;

public class DebuggerWaitTests
{
    private readonly ManualClock _clock = new();
    private int _sleeps;
    private int _attachAfterSleeps = int.MaxValue;

    public DebuggerWait CreateDebuggerWait() => new(() => _sleeps >= _attachAfterSleeps, _clock, Sleep);

    [Fact]
    public void Wait_DebuggerAlreadyAttached_ReturnsTrueWithoutSleeping()
    {
        // Arrange
        _attachAfterSleeps = 0;
        DebuggerWait wait = CreateDebuggerWait();

        // Act
        bool attached = wait.Wait();

        // Assert
        Assert.Equal((true, 0), (attached, _sleeps));
    }

    [Fact]
    public void Wait_DebuggerAttachesWhileWaiting_ReturnsTrue()
    {
        // Arrange
        _attachAfterSleeps = 3;
        DebuggerWait wait = CreateDebuggerWait();

        // Act
        bool attached = wait.Wait();

        // Assert
        Assert.Equal((true, 3), (attached, _sleeps));
    }

    [Fact]
    public void Wait_NoDebuggerBeforeTheLimit_ReturnsFalse()
    {
        // Arrange
        DebuggerWait wait = CreateDebuggerWait();

        // Act
        bool attached = wait.Wait();

        // Assert
        Assert.False(attached);
    }

    [Fact]
    public void Wait_NoDebuggerBeforeTheLimit_StopsAtTheLimit()
    {
        // Arrange
        DebuggerWait wait = CreateDebuggerWait();

        // Act
        wait.Wait();

        // Assert
        Assert.Equal(DebuggerWait.Limit, _sleeps * DebuggerWait.PollInterval);
    }

    private void Sleep(TimeSpan duration)
    {
        _sleeps++;
        _clock.Advance(duration);
    }
}
