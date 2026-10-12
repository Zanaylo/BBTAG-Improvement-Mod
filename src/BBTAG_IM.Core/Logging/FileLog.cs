using System.Globalization;

namespace BBTAG_IM.Core.Logging;

public sealed class FileLog(string path, TimeProvider clock) : ILog
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    private const string InfoLevel = "INFO ";
    private const string ErrorLevel = "ERROR";

    private readonly Lock _gate = new();

    public string Path => path;

    public void Info(string message) => Write(InfoLevel, message);

    public void Error(string message) => Write(ErrorLevel, message);

    private void Write(string level, string message)
    {
        var timestamp = clock.GetLocalNow().ToString(TimestampFormat, CultureInfo.InvariantCulture);

        lock (_gate)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"[{timestamp}] {level} {message}{Environment.NewLine}");
        }
    }
}
