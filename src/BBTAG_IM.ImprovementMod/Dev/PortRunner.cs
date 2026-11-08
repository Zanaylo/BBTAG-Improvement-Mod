using System.ComponentModel;
using System.Diagnostics;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Porting;
using BBTAG_IM.ImprovementMod.Frames;

namespace BBTAG_IM.ImprovementMod.Dev;

internal sealed record PortOutcome(PortJob Job, string? Error);

internal sealed class PortRunner(string program, ILog log)
{
    private const string ThreadName = "Character port";
    private const int Idle = 0;
    private const int Running = 1;

    private PortOutcome? _outcome;
    private int _state;

    public bool IsRunning => Volatile.Read(ref _state) == Running;

    public PortOutcome? Take() => Interlocked.Exchange(ref _outcome, null);

    public void Start(PortJob job, IReadOnlyList<string> arguments)
    {
        if (Interlocked.Exchange(ref _state, Running) == Running)
            return;

        new Thread(() => Run(job, arguments)) { IsBackground = true, Name = ThreadName }.Start();
    }

    private void Run(PortJob job, IReadOnlyList<string> arguments)
    {
        try
        {
            var error = Execute(arguments);
            SafeLog.Info(log, error is null ? $"{job.Character.Name} was built into {job.ModName}." : $"{job.Character.Name} could not be built: {error}");
            Volatile.Write(ref _outcome, new PortOutcome(job, error));
        }
        finally
        {
            Volatile.Write(ref _state, Idle);
        }
    }

    private string? Execute(IReadOnlyList<string> arguments)
    {
        if (!File.Exists(program))
            return $"{program} was not found.";

        var start = new ProcessStartInfo(program)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        try
        {
            return Wait(start);
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or InvalidOperationException)
        {
            return exception.Message;
        }
    }

    private static string? Wait(ProcessStartInfo start)
    {
        using var process = Process.Start(start);

        if (process is null)
            return "The tool did not start.";

        process.BeginOutputReadLine();
        var errors = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();

        if (process.ExitCode == 0)
            return null;

        return errors.Length > 0 ? errors : $"The tool stopped with code {process.ExitCode}.";
    }
}
