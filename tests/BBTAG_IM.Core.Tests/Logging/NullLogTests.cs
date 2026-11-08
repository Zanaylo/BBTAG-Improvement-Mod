using BBTAG_IM.Core.Logging;

namespace BBTAG_IM.Core.Tests.Logging;

public class NullLogTests
{
    [Fact]
    public void Info_AnyMessage_DoesNothing()
    {
        // Arrange
        NullLog log = new();

        // Act
        Exception? exception = Record.Exception(() => log.Info("message"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Error_AnyMessage_DoesNothing()
    {
        // Arrange
        NullLog log = new();

        // Act
        Exception? exception = Record.Exception(() => log.Error("message"));

        // Assert
        Assert.Null(exception);
    }
}
