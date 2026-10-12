namespace BBTAG_IM.Core.Online;

public sealed record P2PSnapshot(IReadOnlyList<ulong> Remotes, IReadOnlyList<int> Channels);

public sealed class P2PTraffic
{
    private readonly Lock _lock = new();
    private readonly HashSet<ulong> _remotes = [];
    private readonly HashSet<int> _channels = [];

    public void Sent(ulong remote, int channel)
    {
        lock (_lock)
        {
            _remotes.Add(remote);
            _channels.Add(channel);
        }
    }

    public P2PSnapshot Take()
    {
        lock (_lock)
        {
            var snapshot = new P2PSnapshot([.. _remotes], [.. _channels]);
            _remotes.Clear();
            _channels.Clear();
            return snapshot;
        }
    }
}
