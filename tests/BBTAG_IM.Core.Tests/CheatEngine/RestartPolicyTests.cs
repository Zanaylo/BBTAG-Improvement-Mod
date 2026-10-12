using BBTAG_IM.Core.CheatEngine;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class RestartPolicyTests
{
    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 119)]
    public void ShouldRestart_CheatEngineDiedSoonAfterAttaching_IsTrue(int attempt, int secondsAttached)
    {
        // Act
        bool restart = RestartPolicy.ShouldRestart(attempt, TimeSpan.FromSeconds(secondsAttached));

        // Assert
        Assert.True(restart);
    }

    [Fact]
    public void ShouldRestart_LastAttempt_IsFalse()
    {
        // Act
        bool restart = RestartPolicy.ShouldRestart(RestartPolicy.MaximumAttempts, TimeSpan.FromSeconds(10));

        // Assert
        Assert.False(restart);
    }

    [Fact]
    public void ShouldRestart_CheatEngineRanForTheWholeStartupWindow_IsFalse()
    {
        // Act
        bool restart = RestartPolicy.ShouldRestart(1, RestartPolicy.StartupWindow);

        // Assert
        Assert.False(restart);
    }
}
