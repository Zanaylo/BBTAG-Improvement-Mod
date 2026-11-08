using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Training;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptFreezeTests
{
    private readonly IMatchPause _matchPauseMock = Substitute.For<IMatchPause>();

    [Fact]
    public void Follow_ScriptWaitsForAFix_PausesTheMatch()
    {
        // Arrange
        _matchPauseMock.When(pause => pause.PauseNow()).Do(_ => _matchPauseMock.IsPaused.Returns(true));
        ScriptFreeze freeze = CreateScriptFreeze();

        // Act
        freeze.Follow(waitsForFix: true);

        // Assert
        _matchPauseMock.Received(1).PauseNow();
        Assert.True(freeze.IsFrozen);
    }

    [Fact]
    public void Follow_StillWaitingOnTheNextFrame_DoesNotPauseAgain()
    {
        // Arrange
        _matchPauseMock.When(pause => pause.PauseNow()).Do(_ => _matchPauseMock.IsPaused.Returns(true));
        ScriptFreeze freeze = CreateScriptFreeze();
        freeze.Follow(waitsForFix: true);

        // Act
        freeze.Follow(waitsForFix: true);

        // Assert
        _matchPauseMock.Received(1).PauseNow();
    }

    [Fact]
    public void Follow_MatchCannotBePaused_TriesAgainOnTheNextFrame()
    {
        // Arrange
        ScriptFreeze freeze = CreateScriptFreeze();
        freeze.Follow(waitsForFix: true);

        // Act
        freeze.Follow(waitsForFix: true);

        // Assert
        _matchPauseMock.Received(2).PauseNow();
        Assert.False(freeze.IsFrozen);
    }

    [Fact]
    public void Follow_FixSaved_ResumesTheMatch()
    {
        // Arrange
        _matchPauseMock.When(pause => pause.PauseNow()).Do(_ => _matchPauseMock.IsPaused.Returns(true));
        ScriptFreeze freeze = CreateScriptFreeze();
        freeze.Follow(waitsForFix: true);

        // Act
        freeze.Follow(waitsForFix: false);

        // Assert
        _matchPauseMock.Received(1).Resume();
        Assert.False(freeze.IsFrozen);
    }

    [Fact]
    public void Follow_PlayerUnpausedWhileWaiting_DoesNotPauseAgainNorResume()
    {
        // Arrange
        _matchPauseMock.When(pause => pause.PauseNow()).Do(_ => _matchPauseMock.IsPaused.Returns(true));
        ScriptFreeze freeze = CreateScriptFreeze();
        freeze.Follow(waitsForFix: true);
        _matchPauseMock.IsPaused.Returns(false);

        // Act
        freeze.Follow(waitsForFix: true);
        freeze.Follow(waitsForFix: false);

        // Assert
        _matchPauseMock.Received(1).PauseNow();
        _matchPauseMock.DidNotReceive().Resume();
        Assert.False(freeze.IsFrozen);
    }

    [Fact]
    public void Follow_NothingWaits_LeavesThePauseAlone()
    {
        // Arrange
        ScriptFreeze freeze = CreateScriptFreeze();

        // Act
        freeze.Follow(waitsForFix: false);

        // Assert
        _matchPauseMock.DidNotReceive().PauseNow();
        _matchPauseMock.DidNotReceive().Resume();
    }

    public ScriptFreeze CreateScriptFreeze() => new(_matchPauseMock);
}
