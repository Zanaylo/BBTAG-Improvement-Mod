using BBTAG_IM.Core.Logging;

namespace BBTAG_IM.Core.Tests.Logging;

public class RepeatedMessagesTests
{
    private const string Time = "18:29:51";
    private const string LaterTime = "18:30:00";
    private const string Assert20 = "BB_Assert NmlAtkAir5C 2427 20 2 FD 0 NmlAtkAir236A";
    private const string Assert21 = "BB_Assert NmlAtkAir5C 2427 21 2 FD 0 NmlAtkAir236A";

    [Fact]
    public void Show_FirstTime_ReturnsTheLineWithItsTimeAndReplacesNothing()
    {
        // Arrange
        RepeatedMessages messages = CreateRepeatedMessages();

        // Act
        ShownMessage shown = messages.Show(Time, "Starting the game.");

        // Assert
        Assert.Equal(new ShownMessage("18:29:51  Starting the game.", null), shown);
    }

    [Fact]
    public void Show_SameMessageAgain_ReplacesTheEarlierLineAndCountsIt()
    {
        // Arrange
        RepeatedMessages messages = CreateRepeatedMessages();
        messages.Show(Time, "Starting the game.");

        // Act
        ShownMessage shown = messages.Show(LaterTime, "Starting the game.");

        // Assert
        Assert.Equal(new ShownMessage("18:30:00  Starting the game. (x2)", "18:29:51  Starting the game."), shown);
    }

    [Fact]
    public void Show_ThirdTime_ReplacesTheCountedLine()
    {
        // Arrange
        RepeatedMessages messages = CreateRepeatedMessages();
        messages.Show(Time, "Starting the game.");
        messages.Show(Time, "Starting the game.");

        // Act
        ShownMessage shown = messages.Show(Time, "Starting the game.");

        // Assert
        Assert.Equal(new ShownMessage("18:29:51  Starting the game. (x3)", "18:29:51  Starting the game. (x2)"), shown);
    }

    [Fact]
    public void Show_MessageThatOnlyChangesANumber_CountsAsTheSameMessageAndShowsTheLatest()
    {
        // Arrange
        RepeatedMessages messages = CreateRepeatedMessages();
        messages.Show(Time, Assert20);

        // Act
        ShownMessage shown = messages.Show(Time, Assert21);

        // Assert
        Assert.Equal(new ShownMessage($"18:29:51  {Assert21} (x2)", $"18:29:51  {Assert20}"), shown);
    }

    [Fact]
    public void Show_OtherMessageBetweenRepeats_StillCountsTheRepeat()
    {
        // Arrange
        RepeatedMessages messages = CreateRepeatedMessages();
        messages.Show(Time, "First.");
        messages.Show(Time, "Second.");

        // Act
        ShownMessage shown = messages.Show(Time, "First.");

        // Assert
        Assert.Equal(new ShownMessage("18:29:51  First. (x2)", "18:29:51  First."), shown);
    }

    [Fact]
    public void Show_OtherHexAddress_IsAnotherMessage()
    {
        // Arrange
        RepeatedMessages messages = CreateRepeatedMessages();
        messages.Show(Time, "Access violation at 0x44A47.");

        // Act
        ShownMessage shown = messages.Show(Time, "Access violation at 0x44A64.");

        // Assert
        Assert.Null(shown.Replaces);
    }

    [Fact]
    public void Show_OtherNumberInsideAName_IsAnotherMessage()
    {
        // Arrange
        RepeatedMessages messages = CreateRepeatedMessages();
        messages.Show(Time, "Action NmlAtkAir236A is missing.");

        // Act
        ShownMessage shown = messages.Show(Time, "Action NmlAtkAir214A is missing.");

        // Assert
        Assert.Null(shown.Replaces);
    }

    public RepeatedMessages CreateRepeatedMessages() => new();
}
