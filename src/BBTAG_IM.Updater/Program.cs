using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Updater;

internal static class Program
{
    private const string SingleInstanceName = @"Local\BBTAG_IM_Updater";

    [STAThread]
    private static void Main(string[] arguments)
    {
        using var singleInstance = new Mutex(initiallyOwned: true, SingleInstanceName, out var isFirst);

        if (!isFirst)
            return;

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        var folder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        new UpdaterApp(folder, UpdaterCommandLine.Parse(arguments), new GitHubApi(http), new PhysicalFileSystem()).Run();
    }
}
