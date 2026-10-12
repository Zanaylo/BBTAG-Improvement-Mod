namespace BBTAG_IM.Core.Logging;

public interface ILog
{
    void Info(string message);

    void Error(string message);
}
