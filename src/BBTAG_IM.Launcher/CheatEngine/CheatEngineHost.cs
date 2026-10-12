using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using BBTAG_IM.Core.CheatEngine;
using BBTAG_IM.Core.Logging;

namespace BBTAG_IM.Launcher.CheatEngine;

internal sealed class CheatEngineHost : IDisposable
{
    private const string SessionFolder = "session";
    private const string RunAsAdminVerb = "runas";
    private const int UserCanceledElevation = 1223;

    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ExitCheck = TimeSpan.FromSeconds(1);

    private readonly Process _process;
    private readonly NamedPipeServerStream _pipe;

    private CheatEngineHost(Process process, NamedPipeServerStream pipe)
    {
        _process = process;
        _pipe = pipe;
        Bridge = new BridgeConnection(pipe);
    }

    public BridgeConnection Bridge { get; }

    public static async Task<CheatEngineHost> StartAsync(CheatEngineLaunch launch, ILog log)
    {
        var sessionTable = WriteSessionTable(launch.TablePath, launch.LauncherDirectory, launch.GameProcessId);
        var pipe = new NamedPipeServerStream(
            BridgeScript.PipeName(launch.GameProcessId), PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        try
        {
            var process = Start(launch.CheatEnginePath, sessionTable, launch.RunAsAdmin);
            log.Info($"Started {launch.CheatEnginePath} with {sessionTable}{(launch.RunAsAdmin ? " as admin" : "")}.");

            try
            {
                await WaitForConnectionAsync(pipe, process);
                log.Info("Connected to Cheat Engine.");
                return new CheatEngineHost(process, pipe);
            }
            catch
            {
                process.Dispose();
                throw;
            }
        }
        catch
        {
            await pipe.DisposeAsync();
            throw;
        }
    }

    public Task WaitForExitAsync() => _process.WaitForExitAsync();

    public int ExitCode => _process.ExitCode;

    public void Close()
    {
        try
        {
            if (!_process.HasExited)
                Bridge.Close();
        }
        catch (BridgeException)
        {
        }
    }

    public void Dispose()
    {
        _pipe.Dispose();
        _process.Dispose();
    }

    private static Process Start(string cheatEnginePath, string sessionTable, bool runAsAdmin)
    {
        var startInfo = new ProcessStartInfo(cheatEnginePath, $"\"{sessionTable}\"")
        {
            WorkingDirectory = Path.GetDirectoryName(cheatEnginePath),
            UseShellExecute = true,
            Verb = runAsAdmin ? RunAsAdminVerb : "",
        };

        try
        {
            return Process.Start(startInfo) ?? throw new BridgeException($"Windows could not start {cheatEnginePath}.");
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == UserCanceledElevation)
        {
            throw new BridgeException("Cheat Engine was not allowed to run as admin.", exception);
        }
    }

    private static string WriteSessionTable(string tablePath, string launcherDirectory, int gameProcessId)
    {
        var sessionTable = Path.Combine(launcherDirectory, SessionFolder, Path.GetFileName(tablePath));
        Directory.CreateDirectory(Path.GetDirectoryName(sessionTable)!);
        File.WriteAllText(sessionTable, SessionTable.Embed(File.ReadAllText(tablePath), BridgeScript.For(gameProcessId)));
        return sessionTable;
    }

    private static async Task WaitForConnectionAsync(NamedPipeServerStream pipe, Process cheatEngine)
    {
        using var timeout = new CancellationTokenSource(ConnectTimeout);
        var connection = pipe.WaitForConnectionAsync(timeout.Token);

        while (!connection.IsCompleted)
        {
            if (cheatEngine.HasExited)
                throw new BridgeException("Cheat Engine closed before it loaded the table.");

            await Task.WhenAny(connection, Task.Delay(ExitCheck));
        }

        try
        {
            await connection;
        }
        catch (OperationCanceledException exception)
        {
            throw new BridgeException("Cheat Engine did not answer. If it asked whether to run the table's Lua script, answer Yes next time.", exception);
        }
    }
}

internal sealed record CheatEngineLaunch(string CheatEnginePath, string TablePath, string LauncherDirectory, int GameProcessId, bool RunAsAdmin);
