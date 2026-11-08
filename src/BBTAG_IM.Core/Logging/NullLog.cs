namespace BBTAG_IM.Core.Logging;

public sealed class NullLog : ILog
{
    public void Info(string message)
    {
    }

    public void Error(string message)
    {
    }
}
