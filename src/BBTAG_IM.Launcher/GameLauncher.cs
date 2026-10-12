using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Launcher.Injection;

namespace BBTAG_IM.Launcher;

internal sealed record LaunchResult(PayloadStatus Status, int ProcessId);

internal sealed class GameLauncher(PayloadInjector injector, ILog log)
{
    private const string SteamAppIdVariable = "SteamAppId";
    private const string SteamGameIdVariable = "SteamGameId";
    private const string PathVariable = "PATH";

    private static readonly string? OriginalPath = Environment.GetEnvironmentVariable(PathVariable);

    public LaunchResult Launch(LaunchSession session, LaunchTarget target)
    {
        var entryPointRva = FileImage.Load(target.ExecutablePath).Headers.AddressOfEntryPoint;

        Environment.SetEnvironmentVariable(SteamAppIdVariable, GameLocator.SteamAppId);
        Environment.SetEnvironmentVariable(SteamGameIdVariable, GameLocator.SteamAppId);
        Environment.SetEnvironmentVariable(PathVariable, session.GameDirectory + Path.PathSeparator + OriginalPath);

        using var process = RemoteProcess.StartSuspended(target.ExecutablePath, session.GameDirectory);
        log.Info($"Started {target.ExecutablePath} in {session.GameDirectory} (process {process.ProcessId}).");

        try
        {
            var gate = EntryPointGate.HoldAtEntryPoint(process, process.ImageBase() + entryPointRva);
            var status = injector.Inject(process, session);

            if (status != PayloadStatus.Ready)
            {
                process.Terminate();
                return new LaunchResult(status, (int)process.ProcessId);
            }

            gate.Release();
            return new LaunchResult(status, (int)process.ProcessId);
        }
        catch
        {
            process.Terminate();
            throw;
        }
    }
}
