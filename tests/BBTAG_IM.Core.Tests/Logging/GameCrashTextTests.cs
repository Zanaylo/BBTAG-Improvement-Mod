using BBTAG_IM.Core.Logging;

namespace BBTAG_IM.Core.Tests.Logging;

public class GameCrashTextTests
{
    [Fact]
    public void Describe_NoMessage_NamesTheCodeAndAddress()
    {
        // Arrange
        string? message = null;

        // Act
        string text = GameCrashText.Describe(0xC0000005, 0x0077CC25, message);

        // Assert
        Assert.Equal("Unhandled exception 0xC0000005 at 0x0077CC25.", text);
    }

    [Fact]
    public void Describe_GameMessage_QuotesIt()
    {
        // Arrange
        string message = "vector too long";

        // Act
        string text = GameCrashText.Describe(0xE06D7363, 0x7644D444, message);

        // Assert
        Assert.Equal("Unhandled exception 0xE06D7363 at 0x7644D444. The game says \"vector too long\".", text);
    }

    [Fact]
    public void Describe_SteamworksMessage_TellsToOpenSteam()
    {
        // Arrange
        string message = "can not initialize steamworks.";

        // Act
        string text = GameCrashText.Describe(0xE06D7363, 0x7644D444, message);

        // Assert
        Assert.Equal(
            "Unhandled exception 0xE06D7363 at 0x7644D444. The game says \"can not initialize steamworks.\". The game could not reach Steam. Open Steam and sign in, then press Play again.",
            text);
    }
}
