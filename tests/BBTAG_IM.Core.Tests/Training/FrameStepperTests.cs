using BBTAG_IM.Core.Training;

namespace BBTAG_IM.Core.Tests.Training;

public class FrameStepperTests
{
    public FrameStepper CreateFrameStepper() => new();

    [Fact]
    public void Hold_NotPaused_LetsTheMatchRun()
    {
        // Arrange
        FrameStepper stepper = CreateFrameStepper();

        // Act
        bool held = stepper.Hold(inTrainingMatch: true);

        // Assert
        Assert.False(held);
    }

    [Fact]
    public void Hold_Paused_HoldsTheMatch()
    {
        // Arrange
        FrameStepper stepper = CreateFrameStepper();
        stepper.Toggle();

        // Act
        bool held = stepper.Hold(inTrainingMatch: true);

        // Assert
        Assert.True(held);
    }

    [Fact]
    public void Hold_PausedTwice_LetsTheMatchRunAgain()
    {
        // Arrange
        FrameStepper stepper = CreateFrameStepper();
        stepper.Toggle();
        stepper.Toggle();

        // Act
        bool held = stepper.Hold(inTrainingMatch: true);

        // Assert
        Assert.False(held);
    }

    [Fact]
    public void Hold_PausedWithAStep_LetsOneFrameRunAndHoldsTheNext()
    {
        // Arrange
        FrameStepper stepper = CreateFrameStepper();
        stepper.Toggle();
        stepper.Step();

        // Act
        bool firstHeld = stepper.Hold(inTrainingMatch: true);
        bool secondHeld = stepper.Hold(inTrainingMatch: true);

        // Assert
        Assert.Equal((false, true), (firstHeld, secondHeld));
    }

    [Fact]
    public void Hold_PausedWithTwoSteps_LetsTwoFramesRun()
    {
        // Arrange
        FrameStepper stepper = CreateFrameStepper();
        stepper.Toggle();
        stepper.Step();
        stepper.Step();

        // Act
        bool[] held = [stepper.Hold(inTrainingMatch: true), stepper.Hold(inTrainingMatch: true), stepper.Hold(inTrainingMatch: true)];

        // Assert
        Assert.Equal([false, false, true], held);
    }

    [Fact]
    public void Step_NotPaused_IsForgotten()
    {
        // Arrange
        FrameStepper stepper = CreateFrameStepper();
        stepper.Step();
        stepper.Toggle();

        // Act
        bool held = stepper.Hold(inTrainingMatch: true);

        // Assert
        Assert.True(held);
    }

    [Fact]
    public void Hold_OutsideATrainingMatch_LetsTheGameRunAndUnpauses()
    {
        // Arrange
        FrameStepper stepper = CreateFrameStepper();
        stepper.Toggle();

        // Act
        bool held = stepper.Hold(inTrainingMatch: false);

        // Assert
        Assert.Equal((false, false), (held, stepper.IsPaused));
    }

    [Fact]
    public void Hold_OutsideATrainingMatch_ForgetsTheStepsLeft()
    {
        // Arrange
        FrameStepper stepper = CreateFrameStepper();
        stepper.Toggle();
        stepper.Step();
        stepper.Hold(inTrainingMatch: false);
        stepper.Toggle();

        // Act
        bool held = stepper.Hold(inTrainingMatch: true);

        // Assert
        Assert.True(held);
    }
}
