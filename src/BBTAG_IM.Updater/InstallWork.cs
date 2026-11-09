using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Updater;

internal sealed class InstallWork(IGitHubApi gitHub, ReleaseInstaller installer, string folder, UpdateOffer offer)
{
    private const string WorkFolderName = "BBTAG_IM_Update";

    private readonly CancellationTokenSource _cancel = new();
    private long _downloaded;
    private volatile bool _installing;
    private Task? _task;

    public long Downloaded => Interlocked.Read(ref _downloaded);

    public long Size => offer.Asset.Size;

    public bool IsInstalling => _installing;

    public bool IsFinished => _task?.IsCompleted == true;

    public string? Failure => _task?.Exception?.InnerException is { } exception ? DescribeFailure(exception) : null;

    public void Start() => _task ??= Task.Run(RunAsync);

    public void Cancel()
    {
        if (!_installing)
            _cancel.Cancel();
    }

    public void WaitUntilStopped()
    {
        try
        {
            _task?.Wait();
        }
        catch (AggregateException)
        {
            return;
        }
    }

    private async Task RunAsync()
    {
        var work = Path.Combine(Path.GetTempPath(), WorkFolderName);
        RemoveFolder(work);
        Directory.CreateDirectory(work);

        try
        {
            var zip = Path.Combine(work, offer.Asset.Name);

            await using (var stream = File.Create(zip))
                await gitHub.DownloadAsync(offer.Asset.DownloadUrl, stream, new DownloadCounter(bytes => Interlocked.Exchange(ref _downloaded, bytes)), _cancel.Token);

            _installing = true;
            installer.Install(zip, folder);
        }
        finally
        {
            RemoveFolder(work);
        }
    }

    private static string DescribeFailure(Exception exception) => exception switch
    {
        UnauthorizedAccessException => "Windows did not let the updater replace the files. Run BBTAG_IM_Updater.exe as administrator and try again.",
        IOException => $"A file could not be replaced: {exception.Message} Close the game and the Mod Loader, then run BBTAG_IM_Updater.exe again.",
        _ => exception.Message,
    };

    private static void RemoveFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }
    }

    private sealed class DownloadCounter(Action<long> report) : IProgress<long>
    {
        public void Report(long value) => report(value);
    }
}
