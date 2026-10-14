using BBTAG_IM.Core.CheatEngine;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class BridgeStatusTests
{
    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 2, 1)]
    public void IsWorking_ScriptsPendingOrRunningWithoutWaiting_IsTrue(int pending, int running, int waiting)
    {
        // Arrange
        BridgeStatus status = new(pending, running, waiting);

        // Act
        bool working = status.IsWorking;

        // Assert
        Assert.True(working);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 3, 3)]
    public void IsWorking_EveryRunningScriptIsWaiting_IsFalse(int pending, int running, int waiting)
    {
        // Arrange
        BridgeStatus status = new(pending, running, waiting);

        // Act
        bool working = status.IsWorking;

        // Assert
        Assert.False(working);
    }
}
