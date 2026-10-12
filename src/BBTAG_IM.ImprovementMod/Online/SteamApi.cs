using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BBTAG_IM.ImprovementMod.Online;

internal static unsafe partial class SteamApi
{
    public const string NetworkingVersion = "SteamNetworking005";
    public const string UserVersion = "SteamUser019";
    public const string FriendsVersion = "SteamFriends015";
    public const string UserStatsVersion = "STEAMUSERSTATS_INTERFACE_VERSION011";
    public const string UtilsVersion = "SteamUtils009";

    private const string Library = "steam_api.dll";

    [LibraryImport(Library, EntryPoint = "SteamClient")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint Client();

    [LibraryImport(Library, EntryPoint = "SteamAPI_GetHSteamUser")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int User();

    [LibraryImport(Library, EntryPoint = "SteamAPI_GetHSteamPipe")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int Pipe();

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamClient_GetISteamNetworking", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint Networking(nint client, int user, int pipe, string version);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamClient_GetISteamUser", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint UserInterface(nint client, int user, int pipe, string version);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamClient_GetISteamFriends", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint Friends(nint client, int user, int pipe, string version);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamClient_GetISteamUserStats", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint UserStats(nint client, int user, int pipe, string version);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamClient_GetISteamUtils", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint Utils(nint client, int pipe, string version);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUserStats_GetNumberOfCurrentPlayers")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ulong RequestCurrentPlayers(nint userStats);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUtils_IsAPICallCompleted")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CallCompleted(nint utils, ulong call, byte* failed);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUtils_GetAPICallResult")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool CallResult(nint utils, ulong call, void* result, int resultLength, int expectedCallback, byte* failed);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamUser_GetSteamID")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ulong SteamId(nint user);

    [LibraryImport(Library, EntryPoint = "SteamAPI_ISteamFriends_GetPersonaName")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* PersonaName(nint friends);
}
