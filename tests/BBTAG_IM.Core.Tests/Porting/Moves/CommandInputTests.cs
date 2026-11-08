using BBTAG_IM.Core.Porting.Moves;

namespace BBTAG_IM.Core.Tests.Porting.Moves;

public class CommandInputTests
{
    [Theory]
    [InlineData("236A", "Move_Input_(INPUT_236)", "Move_Input_(INPUT_PRESS_A)")]
    [InlineData("214C", "Move_Input_(INPUT_214)", "Move_Input_(INPUT_PRESS_C)")]
    [InlineData("202B", "Move_Input_(INPUT_22)", "Move_Input_(INPUT_PRESS_B)")]
    [InlineData("236B+C", "Move_Input_(INPUT_236)", "Move_Input_(INPUT_PRESS_BC)")]
    [InlineData(">+B+C", "Move_Input_(INPUT_HOLD_FORWARD)", "Move_Input_(INPUT_PRESS_BC)")]
    [InlineData("<+B+C", "Move_Input_(INPUT_HOLD_BACK)", "Move_Input_(INPUT_PRESS_BC)")]
    [InlineData("v+B+C", "Move_Input_(INPUT_HOLD_DOWN)", "Move_Input_(INPUT_PRESS_BC)")]
    [InlineData("4+A", "Move_Input_(INPUT_HOLD_4)", "Move_Input_(INPUT_PRESS_A)")]
    public void Requirements_MotionOrDirectionAndButton_NamesBothInputs(string input, string first, string second)
    {
        // Act
        IReadOnlyList<string>? requirements = CommandInput.Requirements(input);

        // Assert
        Assert.Equal([first, second], requirements);
    }

    [Fact]
    public void Requirements_ButtonAlone_OnlyAsksForTheButton()
    {
        // Act
        IReadOnlyList<string>? requirements = CommandInput.Requirements("B");

        // Assert
        Assert.Equal(["Move_Input_(INPUT_PRESS_B)"], requirements);
    }

    [Fact]
    public void Requirements_NeutralDirection_OnlyAsksForTheButton()
    {
        // Act
        IReadOnlyList<string>? requirements = CommandInput.Requirements("0+A");

        // Assert
        Assert.Equal(["Move_Input_(INPUT_PRESS_A)"], requirements);
    }

    [Theory]
    [InlineData("!")]
    [InlineData("A+B+C+D")]
    public void Requirements_InputWithoutABBTAGForm_ReturnsNull(string input)
    {
        // Act
        IReadOnlyList<string>? requirements = CommandInput.Requirements(input);

        // Assert
        Assert.Null(requirements);
    }
}
