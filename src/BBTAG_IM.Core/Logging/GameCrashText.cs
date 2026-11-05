namespace BBTAG_IM.Core.Logging;

public static class GameCrashText
{
    private const string SteamworksMarker = "steamworks";
    private const string SteamHint = " The game could not reach Steam. Open Steam and sign in, then press Play again.";

    public static string Describe(uint code, uint address, string? message)
    {
        var crash = $"Unhandled exception 0x{code:X8} at 0x{address:X8}.";

        if (message is null)
            return crash;

        return $"{crash} The game says \"{message}\".{Hint(message)}";
    }

    private static string Hint(string message) =>
        message.Contains(SteamworksMarker, StringComparison.OrdinalIgnoreCase) ? SteamHint : string.Empty;
}
