namespace BBTAG_IM.Core.Online;

public interface IPlayerCountSource
{
    ulong Request();

    PlayerCountReply Poll(ulong call);
}
