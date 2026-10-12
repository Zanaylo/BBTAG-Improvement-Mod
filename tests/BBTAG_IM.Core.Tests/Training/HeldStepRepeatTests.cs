using BBTAG_IM.Core.Training;

namespace BBTAG_IM.Core.Tests.Training;

public class HeldStepRepeatTests
{
    public HeldStepRepeat CreateHeldStepRepeat() => new();

    [Fact]
    public void Steps_NotHeld_DoesNotStep()
    {
        // Arrange
        HeldStepRepeat repeat = CreateHeldStepRepeat();

        // Act
        bool steps = repeat.Steps(held: false);

        // Assert
        Assert.False(steps);
    }

    [Fact]
    public void Steps_HeldForTheDelay_DoesNotStepYet()
    {
        // Arrange
        HeldStepRepeat repeat = CreateHeldStepRepeat();

        // Act
        int steps = CountSteps(repeat, HeldStepRepeat.DelayFrames);

        // Assert
        Assert.Equal(0, steps);
    }

    [Fact]
    public void Steps_HeldOneFramePastTheDelay_Steps()
    {
        // Arrange
        HeldStepRepeat repeat = CreateHeldStepRepeat();
        CountSteps(repeat, HeldStepRepeat.DelayFrames);

        // Act
        bool steps = repeat.Steps(held: true);

        // Assert
        Assert.True(steps);
    }

    [Fact]
    public void Steps_HeldPastTheDelay_StepsOncePerInterval()
    {
        // Arrange
        HeldStepRepeat repeat = CreateHeldStepRepeat();
        CountSteps(repeat, HeldStepRepeat.DelayFrames);

        // Act
        int steps = CountSteps(repeat, HeldStepRepeat.IntervalFrames * 3);

        // Assert
        Assert.Equal(3, steps);
    }

    [Fact]
    public void Steps_ReleasedAndHeldAgain_WaitsForTheDelayAgain()
    {
        // Arrange
        HeldStepRepeat repeat = CreateHeldStepRepeat();
        CountSteps(repeat, HeldStepRepeat.DelayFrames + 1);
        repeat.Steps(held: false);

        // Act
        int steps = CountSteps(repeat, HeldStepRepeat.DelayFrames);

        // Assert
        Assert.Equal(0, steps);
    }

    private static int CountSteps(HeldStepRepeat repeat, int frames) =>
        Enumerable.Range(0, frames).Count(_ => repeat.Steps(held: true));
}
