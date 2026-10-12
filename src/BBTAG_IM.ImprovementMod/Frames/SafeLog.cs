using BBTAG_IM.Core.Logging;

namespace BBTAG_IM.ImprovementMod.Frames;

internal static class SafeLog
{
    public static void Info(ILog log, string message) => Write(() => log.Info(message));

    public static void Error(ILog log, string message) => Write(() => log.Error(message));

    private static void Write(Action write)
    {
        try
        {
            write();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
