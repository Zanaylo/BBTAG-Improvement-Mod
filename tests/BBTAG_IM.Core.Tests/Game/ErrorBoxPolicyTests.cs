using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Tests.Game;

public class ErrorBoxPolicyTests
{
    [Theory]
    [InlineData("Error", true)]
    [InlineData("Continue", true)]
    [InlineData("BLAZBLUE CROSS TAG BATTLE", false)]
    public void IsGameError_Caption_TellsTheGameErrorBoxesApart(string caption, bool expected)
    {
        // Act
        bool isGameError = ErrorBoxPolicy.IsGameError(caption);

        // Assert
        Assert.Equal(expected, isGameError);
    }

    [Fact]
    public void Decide_ScriptEditCausedIt_RollsTheScriptBack()
    {
        // Act
        ErrorBoxAction action = ErrorBoxPolicy.Decide("Error", () => true, () => true);

        // Assert
        Assert.Equal(ErrorBoxAction.ScriptRolledBack, action);
    }

    [Fact]
    public void Decide_WarningBox_KeepsPlayingWithoutTheBox()
    {
        // Act
        ErrorBoxAction action = ErrorBoxPolicy.Decide("Continue", () => false, () => true);

        // Assert
        Assert.Equal(ErrorBoxAction.Continue, action);
    }

    [Fact]
    public void Decide_FatalBoxDuringAMatch_GoesBackToCharacterSelect()
    {
        // Act
        ErrorBoxAction action = ErrorBoxPolicy.Decide("Error", () => false, () => true);

        // Assert
        Assert.Equal(ErrorBoxAction.BackToCharacterSelect, action);
    }

    [Fact]
    public void Decide_FatalBoxOutsideAMatch_ShowsTheBox()
    {
        // Act
        ErrorBoxAction action = ErrorBoxPolicy.Decide("Error", () => false, () => false);

        // Assert
        Assert.Equal(ErrorBoxAction.ShowBox, action);
    }

    [Fact]
    public void Decide_WarningBox_DoesNotLeaveTheMatch()
    {
        // Arrange
        bool left = false;

        // Act
        ErrorBoxPolicy.Decide("Continue", () => false, () => left = true);

        // Assert
        Assert.False(left);
    }
}
