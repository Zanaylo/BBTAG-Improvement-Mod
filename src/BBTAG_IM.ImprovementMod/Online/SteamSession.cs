using System.Runtime.InteropServices;

namespace BBTAG_IM.ImprovementMod.Online;

internal sealed unsafe record SteamSession(nint Networking, ulong OwnId, string PersonaName)
{
    public static SteamSession? TryConnect()
    {
        var client = SteamApi.Client();

        if (client == 0)
            return null;

        var user = SteamApi.User();
        var pipe = SteamApi.Pipe();
        var networking = SteamApi.Networking(client, user, pipe, SteamApi.NetworkingVersion);
        var steamUser = SteamApi.UserInterface(client, user, pipe, SteamApi.UserVersion);
        var friends = SteamApi.Friends(client, user, pipe, SteamApi.FriendsVersion);

        if (networking == 0 || steamUser == 0 || friends == 0)
            return null;

        return new SteamSession(networking, SteamApi.SteamId(steamUser), Marshal.PtrToStringUTF8((nint)SteamApi.PersonaName(friends)) ?? "");
    }
}
