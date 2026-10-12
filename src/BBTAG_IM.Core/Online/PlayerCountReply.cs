namespace BBTAG_IM.Core.Online;

public enum PlayerCountReplyState
{
    Pending,
    Failed,
    Counted,
}

public readonly record struct PlayerCountReply(PlayerCountReplyState State, int Players)
{
    public static PlayerCountReply Pending => new(PlayerCountReplyState.Pending, 0);

    public static PlayerCountReply Failed => new(PlayerCountReplyState.Failed, 0);

    public static PlayerCountReply Counted(int players) => new(PlayerCountReplyState.Counted, players);
}
