using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Logging;

public class ErrorLogFollowerTests
{
    private const string Path = @"D:\BBTAG\logs\ImprovementMod.log";
    private const string Info = "[2026-10-14 17:00:00.000] INFO  Loaded.\r\n";
    private const string Error = "[2026-10-14 17:00:01.000] ERROR The game raised an error box \"Error\": BB_Assert\r\n file - obj_BBScript.cpp\r\n line - 454\r\n";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void NewErrors_LogWithAnError_ReturnsItWithItsContinuationLines()
    {
        // Arrange
        LogHas(Info + Error);
        ErrorLogFollower follower = CreateErrorLogFollower();

        // Act
        IReadOnlyList<string> errors = follower.NewErrors();

        // Assert
        Assert.Equal(["The game raised an error box \"Error\": BB_Assert file - obj_BBScript.cpp line - 454"], errors);
    }

    [Fact]
    public void NewErrors_SecondCall_ReturnsOnlyWhatWasAdded()
    {
        // Arrange
        LogHas(Error);
        ErrorLogFollower follower = CreateErrorLogFollower();
        follower.NewErrors();
        LogHas(Error + Info + "[2026-10-14 17:00:02.000] ERROR Second.\r\n");

        // Act
        IReadOnlyList<string> errors = follower.NewErrors();

        // Assert
        Assert.Equal(["Second."], errors);
    }

    [Fact]
    public void NewErrors_ErrorStillBeingWritten_WaitsForTheNextEntry()
    {
        // Arrange
        LogHas("[2026-10-14 17:00:01.000] ERROR Half");
        ErrorLogFollower follower = CreateErrorLogFollower();

        // Act
        IReadOnlyList<string> errors = follower.NewErrors();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void NewErrors_LogStartedAgain_ReadsItFromTheStart()
    {
        // Arrange
        LogHas(Info + Info + Info);
        ErrorLogFollower follower = CreateErrorLogFollower();
        follower.NewErrors();
        LogHas("[2026-10-14 17:00:02.000] ERROR New run.\r\n");

        // Act
        IReadOnlyList<string> errors = follower.NewErrors();

        // Assert
        Assert.Equal(["New run."], errors);
    }

    [Fact]
    public void NewErrors_NoLogYet_ReturnsNothing()
    {
        // Arrange
        _fileSystemMock.FileExists(Path).Returns(false);
        ErrorLogFollower follower = CreateErrorLogFollower();

        // Act
        IReadOnlyList<string> errors = follower.NewErrors();

        // Assert
        Assert.Empty(errors);
    }

    public ErrorLogFollower CreateErrorLogFollower() => new(_fileSystemMock, Path);

    private void LogHas(string text)
    {
        _fileSystemMock.FileExists(Path).Returns(true);
        _fileSystemMock.ReadAllText(Path).Returns(text);
    }
}
