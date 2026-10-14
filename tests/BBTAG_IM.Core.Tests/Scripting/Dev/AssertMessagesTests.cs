using BBTAG_IM.Core.Scripting.Dev;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class AssertMessagesTests
{
    [Fact]
    public void Describe_MissingLabel_SaysWhichLabelInEnglish()
    {
        // Arrange
        string raw = "BB_Assert\n file - ..\\..\\BBSrc\\Scene\\Battle\\Object\\obj_BBScript.cpp\n line - 211\n存在しないLabel 0x0063 にGotoしています。\n";

        // Act
        string message = AssertMessages.Describe(raw);

        // Assert
        Assert.Equal("This jumps to label 99, which does not exist in this state.", message);
    }

    [Fact]
    public void Describe_UnregisteredCommand_SaysWhichCommand()
    {
        // Arrange
        string raw = "BB_Assert\n file - x.cpp\n line - 170\n0x9c53は登録されていないコマンドIDです。\n";

        // Act
        string message = AssertMessages.Describe(raw);

        // Assert
        Assert.Equal("Command 40019 is not one the game knows.", message);
    }

    [Fact]
    public void Describe_MissingState_SaysWhichState()
    {
        // Arrange
        string raw = "BB_Assert\n file - x.cpp\n line - 454\nNmlAtk5A 3 0 0 FD 0\nアクションがないか、つづりを間違えています。NmlAtk5X\n";

        // Act
        string message = AssertMessages.Describe(raw);

        // Assert
        Assert.Equal("There is no state named NmlAtk5X, or it is misspelled.", message);
    }

    [Fact]
    public void Describe_UnknownAssert_KeepsTheGameText()
    {
        // Arrange
        string raw = "BB_Assert\n file - x.cpp\n line - 9\nSomething else\n";

        // Act
        string message = AssertMessages.Describe(raw);

        // Assert
        Assert.Equal("Something else", message);
    }
}
