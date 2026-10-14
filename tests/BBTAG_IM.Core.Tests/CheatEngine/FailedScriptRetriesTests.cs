using BBTAG_IM.Core.CheatEngine;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class FailedScriptRetriesTests
{
    public FailedScriptRetries CreateFailedScriptRetries() => new();

    [Fact]
    public void SelectForRetry_FailuresSeenForTheFirstTime_ReturnsThemInOrder()
    {
        // Arrange
        FailedScriptRetries retries = CreateFailedScriptRetries();

        // Act
        IReadOnlyList<int> selected = retries.SelectForRetry([21539, 3430]);

        // Assert
        Assert.Equal([21539, 3430], selected);
    }

    [Fact]
    public void SelectForRetry_ScriptAlreadyRetried_IsNotRetriedAgain()
    {
        // Arrange
        FailedScriptRetries retries = CreateFailedScriptRetries();
        retries.SelectForRetry([21539]);

        // Act
        IReadOnlyList<int> selected = retries.SelectForRetry([21539, 3430]);

        // Assert
        Assert.Equal([3430], selected);
    }

    [Fact]
    public void SelectForRetry_AfterRetriesEnded_ReturnsEmpty()
    {
        // Arrange
        FailedScriptRetries retries = CreateFailedScriptRetries();
        retries.EndRetries();

        // Act
        IReadOnlyList<int> selected = retries.SelectForRetry([21565]);

        // Assert
        Assert.Empty(selected);
    }

    [Fact]
    public void SelectForRetry_NothingFailed_ReturnsEmpty()
    {
        // Arrange
        FailedScriptRetries retries = CreateFailedScriptRetries();

        // Act
        IReadOnlyList<int> selected = retries.SelectForRetry([]);

        // Assert
        Assert.Empty(selected);
    }
}
