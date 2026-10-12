using BBTAG_IM.Core.Selection;

namespace BBTAG_IM.Core.Tests.Selection;

public class WheelChoiceTests
{
    [Theory]
    [InlineData(1f, -1)]
    [InlineData(-1f, 1)]
    [InlineData(0f, 0)]
    public void Steps_WheelTurn_GoesBackUpAndForwardDown(float wheel, int expected)
    {
        // Act
        int steps = WheelChoice.Steps(wheel);

        // Assert
        Assert.Equal(expected, steps);
    }

    [Fact]
    public void Next_StepForward_ChoosesTheNextItem()
    {
        // Act
        int next = WheelChoice.Next(current: 2, count: 5, steps: 1);

        // Assert
        Assert.Equal(3, next);
    }

    [Fact]
    public void Next_StepBackOnTheFirstItem_StaysOnIt()
    {
        // Act
        int next = WheelChoice.Next(current: 0, count: 5, steps: -1);

        // Assert
        Assert.Equal(0, next);
    }

    [Fact]
    public void Next_StepForwardOnTheLastItem_StaysOnIt()
    {
        // Act
        int next = WheelChoice.Next(current: 4, count: 5, steps: 1);

        // Assert
        Assert.Equal(4, next);
    }

    [Fact]
    public void Next_NothingChosenStepForward_ChoosesTheFirstItem()
    {
        // Act
        int next = WheelChoice.Next(current: -1, count: 5, steps: 1);

        // Assert
        Assert.Equal(0, next);
    }

    [Fact]
    public void Next_EmptyList_KeepsTheCurrentChoice()
    {
        // Act
        int next = WheelChoice.Next(current: -1, count: 0, steps: 1);

        // Assert
        Assert.Equal(-1, next);
    }
}
