namespace BBTAG_IM.Launcher.Debugging;

internal sealed class AutomationException(string message) : Exception(message)
{
    public static void ThrowIfFailed(int result)
    {
        if (result < 0)
            throw new AutomationException($"Visual Studio answered 0x{result:X8}.");
    }
}
