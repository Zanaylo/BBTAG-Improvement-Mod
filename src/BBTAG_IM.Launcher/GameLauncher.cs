using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Launcher.Injection;

namespace BBTAG_IM.Launcher;

internal sealed class GameLauncher(PayloadInjector injector, ILog log)
{
    private const string SteamAppIdVariable = "SteamAppId";
    private const string SteamGameIdVariable = "SteamGameId";

    public PayloadStatus Launch(LaunchSession session)
    {
        var executable = Path.Combine(session.GameDirectory, ModFolder.GameExecutableName);
        var entryPointRva = FileImage.Load(executable).Headers.AddressOfEntryPoint;

        Environment.SetEnvironmentVariable(SteamAppIdVariable, GameLocator.SteamAppId);
        Environment.SetEnvironmentVariable(SteamGameIdVariable, GameLocator.SteamAppId);

        using var process = RemoteProcess.StartSuspended(executable, session.GameDirectory);
        log.Info($"Started {executable} (process {process.ProcessId}).");

        try
        {
            var gate = EntryPointGate.HoldAtEntryPoint(process, process.ImageBase() + entryPointRva);
            var status = injector.Inject(process, session);

            if (status != PayloadStatus.Ready)
            {
                process.Terminate();
                return status;
            }

            gate.Release();
            return status;
        }
        catch
        {
            process.Terminate();
            throw;
        }
    }
}
