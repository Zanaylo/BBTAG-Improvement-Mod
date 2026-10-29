using System.ComponentModel;
using System.Diagnostics;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Dev;

internal static unsafe class ScriptEditor
{
    private const string Notepad = "notepad.exe";
    private const string ThreadName = "Script editor";

    private static readonly TimeSpan ProjectLoadTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan WindowCheckInterval = TimeSpan.FromMilliseconds(500);

    public static void Open(string script, int line, string? program, string? project, ILog log)
    {
        var thread = new Thread(() => OpenNow(script, line, program, project, log)) { IsBackground = true, Name = ThreadName };

        if (OperatingSystem.IsWindows())
            thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
    }

    private static void OpenNow(string script, int line, string? program, string? project, ILog log)
    {
        try
        {
            if (program is null)
            {
                AskForProgram(script, line, log);
                return;
            }

            if (!File.Exists(program))
            {
                SafeLog.Error(log, $"{program} was not found, so the Open with window asks for another program.");
                AskForProgram(script, line, log);
                return;
            }

            OpenProjectFirst(program, project);
            Start(program, ScriptEditorProgram.Arguments(program, script, line, project));
        }
        catch (Win32Exception exception)
        {
            SafeLog.Error(log, $"{Path.GetFileName(script)} could not be opened: {exception.Message}");
        }
    }

    private static void OpenProjectFirst(string program, string? project)
    {
        var running = Process.GetProcessesByName(ScriptEditorProgram.VisualStudioProcess).Length > 0;

        if (ScriptEditorProgram.ProjectArguments(program, project, running) is not { } arguments)
            return;

        using var process = Process.Start(new ProcessStartInfo(program, arguments) { UseShellExecute = false });

        if (process is null)
            return;

        var deadline = DateTime.UtcNow + ProjectLoadTimeout;

        while (DateTime.UtcNow < deadline && !process.HasExited && process.MainWindowHandle == 0)
        {
            Thread.Sleep(WindowCheckInterval);
            process.Refresh();
        }
    }

    private static void AskForProgram(string script, int line, ILog log)
    {
        var result = ShowOpenWith(script);

        if (result >= 0 || result == Shell32.Cancelled)
            return;

        SafeLog.Error(log, $"The Open with window could not be shown (0x{result:X8}), so {Path.GetFileName(script)} opens in Notepad.");
        Start(Notepad, ScriptEditorProgram.Arguments(Notepad, script, line));
    }

    private static int ShowOpenWith(string script)
    {
        fixed (char* file = script)
        {
            var information = new OpenAsInfo
            {
                File = (nint)file,
                Flags = Shell32.OpenWithExecute | Shell32.OpenWithHideAlwaysUse,
            };

            return Shell32.OpenWithDialog(0, ref information);
        }
    }

    private static void Start(string program, string arguments) =>
        Process.Start(new ProcessStartInfo(program, arguments) { UseShellExecute = false, CreateNoWindow = true })?.Dispose();
}
