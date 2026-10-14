using System.Diagnostics;
using BBTAG_IM.Core.CheatEngine;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Tables;
using BBTAG_IM.Launcher.CheatEngine;
using BBTAG_IM.Launcher.Debugging;
using BBTAG_IM.Launcher.Injection;
using BBTAG_IM.Launcher.Ui;

namespace BBTAG_IM.Launcher;

internal sealed class GameSession(LaunchContext context, LauncherSettings settings, ILauncherView view) : IDisposable
{
    private const string ProgramFiles64Variable = "ProgramW6432";

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan WindowWait = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan WindowCheck = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan SettleAfterWindow = TimeSpan.FromSeconds(3);

    private CheatEngineHost? _host;
    private CancellationTokenSource? _polling;
    private ILog? _log;

    public async Task RunAsync(Func<IReadOnlyCollection<int>> selection)
    {
        if (File.Exists(context.LogPath))
            File.Delete(context.LogPath);

        _log = new FileLog(context.LogPath, TimeProvider.System);

        try
        {
            for (var attempt = 1; await RunAttemptAsync(attempt, selection, _log); attempt++)
                CloseCheatEngine();
        }
        finally
        {
            CloseCheatEngine();
        }
    }

    public async Task ToggleAsync(int scriptId, bool turnOn)
    {
        var bridge = _host?.Bridge;

        if (bridge is null)
            return;

        await Task.Run(() => Toggle(bridge, scriptId, turnOn));
    }

    public async Task ShowWindowAsync(bool visible)
    {
        var bridge = _host?.Bridge;

        if (bridge is null)
            return;

        await Task.Run(() => bridge.ShowWindow(visible));
    }

    public void Dispose() => CloseCheatEngine();

    private async Task<bool> RunAttemptAsync(int attempt, Func<IReadOnlyCollection<int>> selection, ILog log)
    {
        Report(attempt == 1 ? "Starting the game." : $"Starting the game again (try {attempt} of {RestartPolicy.MaximumAttempts}).");

        if (settings.WaitForDebugger)
            Report($"WaitForDebugger is on. Attach Visual Studio to BBTAG.exe with Native code. The game waits up to {DebuggerWait.Limit.TotalMinutes:0} minutes.");

        var result = await Task.Run(() => StartGame(log));

        if (result.DebuggerNote is { } debuggerNote)
            Report(debuggerNote);

        if (result.Status != PayloadStatus.Ready)
        {
            Report($"{PayloadStatusText.Describe(result.Status)} The game was closed. Details are in {context.LogPath}.");
            return false;
        }

        using var game = Process.GetProcessById(result.ProcessId);
        var gameExit = game.WaitForExitAsync();
        Report("The game is running.");

        var attachedAt = context.Table is null ? null : await AttachCheatEngineAsync(game, context.Table, selection(), log);

        if (attachedAt is null || _host is null)
            return await GameClosedAsync(game, gameExit);

        if (await Task.WhenAny(gameExit, _host.WaitForExitAsync()) == gameExit)
            return await GameClosedAsync(game, gameExit);

        log.Info($"Cheat Engine closed with exit code 0x{_host.ExitCode:X8}.");
        CloseCheatEngine();

        if (!RestartPolicy.ShouldRestart(attempt, DateTime.UtcNow - attachedAt.Value))
        {
            Report("Cheat Engine was closed. The game keeps running without the table.");
            return await GameClosedAsync(game, gameExit);
        }

        Report("Cheat Engine crashed while turning on the scripts. Closing the game to start both again.");

        if (!game.HasExited)
            game.Kill();

        await gameExit;
        return true;
    }

    private async Task<bool> GameClosedAsync(Process game, Task gameExit)
    {
        await gameExit;
        Report($"The game was closed with exit code 0x{game.ExitCode:X8}.");
        return false;
    }

    private LaunchResult StartGame(ILog log)
    {
        log.Info($"Game: {context.GameDirectory}");

        foreach (var mod in context.Mods)
            log.Info($"Mod: {mod.Name}");

        var session = new LaunchSession(context.GameDirectory, context.LogPath, context.Mods.Select(mod => mod.RootPath).ToArray(), settings.WaitForDebugger);
        var injector = new PayloadInjector(Path.Combine(context.LauncherDirectory, PayloadContract.DllFileName));
        return new GameLauncher(injector, new VisualStudioAttacher(), log).Launch(session, context.Target);
    }

    private async Task<DateTime?> AttachCheatEngineAsync(Process game, CheatTable table, IReadOnlyCollection<int> toEnable, ILog log)
    {
        var cheatEngine = new CheatEngineLocator(new PhysicalFileSystem()).Locate(settings.CheatEnginePath, ProgramFilesDirectories());

        if (cheatEngine is null)
        {
            Report($"Cheat Engine was not found. Install it, or write its folder in CheatEnginePath inside {LauncherSettings.FileName}.");
            return null;
        }

        DateTime? attachedAt = null;

        try
        {
            Report("Waiting for the game window.");

            if (!await WaitForWindowAsync(game))
                return null;

            Report(settings.RunCheatEngineAsAdmin ? "Starting Cheat Engine as admin. Accept the Windows prompt." : "Starting Cheat Engine.");
            _host = await CheatEngineHost.StartAsync(
                new CheatEngineLaunch(cheatEngine, context.TablePath!, context.LauncherDirectory, game.Id, settings.RunCheatEngineAsAdmin), log);
            attachedAt = DateTime.UtcNow;
            var bridge = _host.Bridge;

            if (!settings.ShowCheatEngineWindow)
                await Task.Run(() => bridge.ShowWindow(false));

            Report($"Cheat Engine is attached. Turning on {toEnable.Count} script(s). Stay in the menus until they are on.");
            var enabling = Stopwatch.StartNew();
            await Task.Run(() => EnableAll(bridge, toEnable));
            var state = await WaitForScriptsAsync(bridge, table, game, enabling);
            await ReportScriptsAsync(bridge, table, toEnable, state, enabling.Elapsed);
            StartPolling(bridge);
            return attachedAt;
        }
        catch (Exception exception) when (exception is BridgeException or InvalidSessionTableException or IOException)
        {
            Report(exception.Message);

            if (attachedAt is not null)
                return attachedAt;

            Report("The game keeps running without the table.");
            CloseCheatEngine();
            return null;
        }
    }

    private async Task<ActivationState> WaitForScriptsAsync(BridgeConnection bridge, CheatTable table, Process game, Stopwatch enabling)
    {
        var activation = new ScriptActivation();
        var retries = new FailedScriptRetries();

        while (!game.HasExited)
        {
            var state = activation.Observe(await Task.Run(bridge.Status), enabling.Elapsed);

            if (state == ActivationState.TimedOut)
                return state;

            if (state != ActivationState.TurningOn)
            {
                if (!await ActOnQuietScriptsAsync(bridge, table, retries, state))
                    return state;

                activation.Reset();
            }

            view.ShowStatus($"Turning on the scripts ({enabling.Elapsed.TotalSeconds:0} s). Stay in the menus. Starting a match now crashes the game.");
            await Task.Delay(PollInterval);
        }

        return ActivationState.TurningOn;
    }

    private async Task<bool> ActOnQuietScriptsAsync(BridgeConnection bridge, CheatTable table, FailedScriptRetries retries, ActivationState state)
    {
        var failures = await Task.Run(bridge.Failures);
        var retry = retries.SelectForRetry(failures.Select(failure => failure.Id));

        if (retry.Count > 0)
        {
            LogFailures(table, failures);
            Report($"Turning {retry.Count} script(s) on again: {Describe(table, retry)}.");
            await Task.Run(() => EnableAll(bridge, retry));
            return true;
        }

        if (state != ActivationState.Stuck)
            return false;

        Report("Some scripts wait for scripts that did not turn on. They stop waiting now.");
        retries.EndRetries();
        await Task.Run(bridge.CancelWaits);
        return true;
    }

    private async Task ReportScriptsAsync(BridgeConnection bridge, CheatTable table, IReadOnlyCollection<int> requested, ActivationState state, TimeSpan elapsed)
    {
        if (state == ActivationState.TurningOn)
            return;

        var active = await Task.Run(bridge.ActiveScripts);
        view.ShowActiveScripts(active);
        var failures = await Task.Run(bridge.Failures);
        LogFailures(table, failures);

        if (state == ActivationState.TimedOut)
        {
            Report($"Cheat Engine is still turning scripts on after {elapsed.TotalMinutes:0} min. Starting a match may crash the game.");
            return;
        }

        var off = requested.Where(scriptId => !active.Contains(scriptId)).Concat(failures.Select(failure => failure.Id)).Distinct().ToArray();

        if (off.Length > 0)
        {
            Report($"These scripts did not turn on: {Describe(table, off)}. A match may crash. Close the game and start it again from the launcher.");
            return;
        }

        Report($"The scripts are on after {elapsed.TotalSeconds:0} s ({active.Count} active). You can start a match now.");
    }

    private void LogFailures(CheatTable table, IEnumerable<ScriptFailure> failures)
    {
        foreach (var failure in failures)
            _log?.Info($"\"{table.Describe(failure.Id)}\" did not turn on. Cheat Engine says: {failure.Reason}");
    }

    private static string Describe(CheatTable table, IEnumerable<int> scriptIds) =>
        string.Join(", ", scriptIds.Select(table.Describe));

    private void StartPolling(BridgeConnection bridge)
    {
        _polling = new CancellationTokenSource();
        _ = PollAsync(bridge, _polling.Token);
    }

    private async Task PollAsync(BridgeConnection bridge, CancellationToken cancellation)
    {
        using var timer = new PeriodicTimer(PollInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellation))
                view.ShowActiveScripts(await Task.Run(bridge.ActiveScripts, cancellation));
        }
        catch (Exception exception) when (exception is BridgeException or OperationCanceledException)
        {
        }
    }

    private static async Task<bool> WaitForWindowAsync(Process game)
    {
        var deadline = DateTime.UtcNow + WindowWait;

        while (DateTime.UtcNow < deadline)
        {
            game.Refresh();

            if (game.HasExited)
                return false;

            if (game.MainWindowHandle != 0)
            {
                await Task.Delay(SettleAfterWindow);
                return !game.HasExited;
            }

            await Task.Delay(WindowCheck);
        }

        return false;
    }

    private static void EnableAll(BridgeConnection bridge, IEnumerable<int> scriptIds)
    {
        foreach (var scriptId in scriptIds)
            bridge.Enable(scriptId);
    }

    private static void Toggle(BridgeConnection bridge, int scriptId, bool turnOn)
    {
        if (turnOn)
        {
            bridge.Enable(scriptId);
            return;
        }

        bridge.Disable(scriptId);
    }

    private void CloseCheatEngine()
    {
        _polling?.Cancel();
        _polling?.Dispose();
        _polling = null;
        _host?.Close();
        _host?.Dispose();
        _host = null;
    }

    private void Report(string message)
    {
        _log?.Info(message);
        view.Report(message);
    }

    private static IEnumerable<string> ProgramFilesDirectories() =>
        new[]
        {
            Environment.GetEnvironmentVariable(ProgramFiles64Variable),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        }
        .OfType<string>()
        .Where(directory => directory.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase);
}
