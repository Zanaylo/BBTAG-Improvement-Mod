using BBTAG_IM.Core.Online;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Online;

public class PlayerCountTests
{
    private const ulong Call = 42;
    private const int Players = 137;

    private static readonly TimeSpan Moment = TimeSpan.FromSeconds(1);

    private readonly IPlayerCountSource _sourceMock = Substitute.For<IPlayerCountSource>();
    private readonly ManualClock _clock = new();

    public PlayerCount CreatePlayerCount() => new(_sourceMock, _clock);

    [Fact]
    public void Players_BeforeAnyUpdate_IsUnknown()
    {
        // Arrange
        PlayerCount count = CreatePlayerCount();

        // Act
        int? players = count.Players;

        // Assert
        Assert.Null(players);
        Assert.Equal(PlayerCount.NotAskedYet, count.Status);
    }

    [Fact]
    public void Update_FirstFrame_AsksSteam()
    {
        // Arrange
        _sourceMock.Request().Returns(Call);
        PlayerCount count = CreatePlayerCount();

        // Act
        count.Update();

        // Assert
        _sourceMock.Received(1).Request();
        Assert.Equal(PlayerCount.Asking, count.Status);
    }

    [Fact]
    public void Update_SteamRefusesTheRequest_ShowsSteamIsNotReady()
    {
        // Arrange
        _sourceMock.Request().Returns(PlayerCount.NoCall);
        PlayerCount count = CreatePlayerCount();

        // Act
        count.Update();

        // Assert
        Assert.Equal(PlayerCount.SteamNotReady, count.Status);
    }

    [Fact]
    public void Update_SteamRefusedBeforeTheRetryInterval_DoesNotAskAgain()
    {
        // Arrange
        _sourceMock.Request().Returns(PlayerCount.NoCall);
        PlayerCount count = CreatePlayerCount();
        count.Update();
        _clock.Advance(PlayerCount.RetryInterval - Moment);

        // Act
        count.Update();

        // Assert
        _sourceMock.Received(1).Request();
    }

    [Fact]
    public void Update_SteamRefusedAndTheRetryIntervalPassed_AsksAgain()
    {
        // Arrange
        _sourceMock.Request().Returns(PlayerCount.NoCall);
        PlayerCount count = CreatePlayerCount();
        count.Update();
        _clock.Advance(PlayerCount.RetryInterval);

        // Act
        count.Update();

        // Assert
        _sourceMock.Received(2).Request();
    }

    [Fact]
    public void Update_AnswerStillPending_WaitsWithoutAskingAgain()
    {
        // Arrange
        _sourceMock.Request().Returns(Call);
        _sourceMock.Poll(Call).Returns(PlayerCountReply.Pending);
        PlayerCount count = CreatePlayerCount();
        count.Update();
        _clock.Advance(PlayerCount.RefreshInterval);

        // Act
        count.Update();

        // Assert
        _sourceMock.Received(1).Request();
        Assert.Null(count.Players);
        Assert.Equal(PlayerCount.Asking, count.Status);
    }

    [Fact]
    public void Update_CountArrives_KeepsThePlayers()
    {
        // Arrange
        _sourceMock.Request().Returns(Call);
        _sourceMock.Poll(Call).Returns(PlayerCountReply.Counted(Players));
        PlayerCount count = CreatePlayerCount();
        count.Update();

        // Act
        count.Update();

        // Assert
        Assert.Equal(Players, count.Players);
    }

    [Fact]
    public void Update_SteamCannotAnswer_ShowsNoAnswer()
    {
        // Arrange
        _sourceMock.Request().Returns(Call);
        _sourceMock.Poll(Call).Returns(PlayerCountReply.Failed);
        PlayerCount count = CreatePlayerCount();
        count.Update();

        // Act
        count.Update();

        // Assert
        Assert.Null(count.Players);
        Assert.Equal(PlayerCount.NoAnswer, count.Status);
    }

    [Fact]
    public void Update_SteamCouldNotAnswerAndTheRetryIntervalPassed_AsksAgain()
    {
        // Arrange
        _sourceMock.Request().Returns(Call);
        _sourceMock.Poll(Call).Returns(PlayerCountReply.Failed);
        PlayerCount count = CreatePlayerCount();
        count.Update();
        count.Update();
        _clock.Advance(PlayerCount.RetryInterval);

        // Act
        count.Update();

        // Assert
        _sourceMock.Received(2).Request();
    }

    [Fact]
    public void Update_CountedBeforeTheRefreshInterval_DoesNotAskAgain()
    {
        // Arrange
        _sourceMock.Request().Returns(Call);
        _sourceMock.Poll(Call).Returns(PlayerCountReply.Counted(Players));
        PlayerCount count = CreatePlayerCount();
        count.Update();
        count.Update();
        _clock.Advance(PlayerCount.RefreshInterval - Moment);

        // Act
        count.Update();

        // Assert
        _sourceMock.Received(1).Request();
    }

    [Fact]
    public void Update_CountedAndTheRefreshIntervalPassed_AsksAgain()
    {
        // Arrange
        _sourceMock.Request().Returns(Call);
        _sourceMock.Poll(Call).Returns(PlayerCountReply.Counted(Players));
        PlayerCount count = CreatePlayerCount();
        count.Update();
        count.Update();
        _clock.Advance(PlayerCount.RefreshInterval);

        // Act
        count.Update();

        // Assert
        _sourceMock.Received(2).Request();
    }

    [Fact]
    public void Update_RefreshFails_KeepsTheLastCount()
    {
        // Arrange
        _sourceMock.Request().Returns(Call);
        _sourceMock.Poll(Call).Returns(PlayerCountReply.Counted(Players), PlayerCountReply.Failed);
        PlayerCount count = CreatePlayerCount();
        count.Update();
        count.Update();
        _clock.Advance(PlayerCount.RefreshInterval);
        count.Update();

        // Act
        count.Update();

        // Assert
        Assert.Equal(Players, count.Players);
    }
}
