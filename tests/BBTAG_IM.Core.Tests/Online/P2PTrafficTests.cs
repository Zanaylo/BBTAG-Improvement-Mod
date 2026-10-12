using BBTAG_IM.Core.Online;

namespace BBTAG_IM.Core.Tests.Online;

public class P2PTrafficTests
{
    private const ulong Opponent = 76561198000000001;
    private const ulong Spectator = 76561198000000002;

    public P2PTraffic CreateP2PTraffic() => new();

    [Fact]
    public void Take_NothingSent_IsEmpty()
    {
        // Arrange
        P2PTraffic traffic = CreateP2PTraffic();

        // Act
        P2PSnapshot snapshot = traffic.Take();

        // Assert
        Assert.Empty(snapshot.Remotes);
    }

    [Fact]
    public void Take_SameRemoteTwice_ListsItOnce()
    {
        // Arrange
        P2PTraffic traffic = CreateP2PTraffic();
        traffic.Sent(Opponent, channel: 0);
        traffic.Sent(Opponent, channel: 0);

        // Act
        P2PSnapshot snapshot = traffic.Take();

        // Assert
        Assert.Equal([Opponent], snapshot.Remotes);
    }

    [Fact]
    public void Take_TwoRemotes_ListsBoth()
    {
        // Arrange
        P2PTraffic traffic = CreateP2PTraffic();
        traffic.Sent(Opponent, channel: 0);
        traffic.Sent(Spectator, channel: 0);

        // Act
        P2PSnapshot snapshot = traffic.Take();

        // Assert
        Assert.Equal([Opponent, Spectator], snapshot.Remotes.Order());
    }

    [Fact]
    public void Take_SeveralChannels_ListsEachChannelOnce()
    {
        // Arrange
        P2PTraffic traffic = CreateP2PTraffic();
        traffic.Sent(Opponent, channel: 1);
        traffic.Sent(Opponent, channel: 3);
        traffic.Sent(Opponent, channel: 1);

        // Act
        P2PSnapshot snapshot = traffic.Take();

        // Assert
        Assert.Equal([1, 3], snapshot.Channels.Order());
    }

    [Fact]
    public void Take_Twice_StartsAgainFromNothing()
    {
        // Arrange
        P2PTraffic traffic = CreateP2PTraffic();
        traffic.Sent(Opponent, channel: 0);
        traffic.Take();

        // Act
        P2PSnapshot snapshot = traffic.Take();

        // Assert
        Assert.Empty(snapshot.Remotes);
    }
}
