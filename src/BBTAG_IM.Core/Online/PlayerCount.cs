namespace BBTAG_IM.Core.Online;

public sealed class PlayerCount(IPlayerCountSource source, TimeProvider clock)
{
    public const ulong NoCall = 0;
    public const string NotAskedYet = "Not asked yet";
    public const string Asking = "Asking Steam";
    public const string SteamNotReady = "Steam is not ready";
    public const string NoAnswer = "Steam could not answer";

    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);

    private ulong _call = NoCall;
    private DateTimeOffset? _nextRequest;

    public int? Players { get; private set; }

    public string Status { get; private set; } = NotAskedYet;

    public void Update()
    {
        if (_call != NoCall)
        {
            Collect();
            return;
        }

        if (_nextRequest > clock.GetUtcNow())
            return;

        Request();
    }

    private void Request()
    {
        _nextRequest = clock.GetUtcNow() + RetryInterval;
        _call = source.Request();
        Status = _call == NoCall ? SteamNotReady : Asking;
    }

    private void Collect()
    {
        var reply = source.Poll(_call);

        if (reply.State == PlayerCountReplyState.Pending)
            return;

        _call = NoCall;

        if (reply.State == PlayerCountReplyState.Failed)
        {
            Status = NoAnswer;
            return;
        }

        Players = reply.Players;
        _nextRequest = clock.GetUtcNow() + RefreshInterval;
    }
}
