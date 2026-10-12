using System.Runtime.InteropServices;
using BBTAG_IM.Core.Online;

namespace BBTAG_IM.ImprovementMod.Online;

internal sealed unsafe class SteamPlayerCountSource : IPlayerCountSource
{
    private const int CurrentPlayersCallback = 1107;

    private nint _userStats;
    private nint _utils;

    public ulong Request() => Connect() ? SteamApi.RequestCurrentPlayers(_userStats) : PlayerCount.NoCall;

    public PlayerCountReply Poll(ulong call)
    {
        byte failed = 0;

        if (!SteamApi.CallCompleted(_utils, call, &failed))
            return failed == 0 ? PlayerCountReply.Pending : PlayerCountReply.Failed;

        CurrentPlayers result = default;

        if (!SteamApi.CallResult(_utils, call, &result, sizeof(CurrentPlayers), CurrentPlayersCallback, &failed))
            return PlayerCountReply.Failed;

        return failed == 0 && result.Success != 0 ? PlayerCountReply.Counted(result.Players) : PlayerCountReply.Failed;
    }

    private bool Connect()
    {
        if (_userStats != 0 && _utils != 0)
            return true;

        var client = SteamApi.Client();

        if (client == 0)
            return false;

        var pipe = SteamApi.Pipe();
        _userStats = SteamApi.UserStats(client, SteamApi.User(), pipe, SteamApi.UserStatsVersion);
        _utils = SteamApi.Utils(client, pipe, SteamApi.UtilsVersion);
        return _userStats != 0 && _utils != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CurrentPlayers
    {
        public byte Success;
        public int Players;
    }
}
