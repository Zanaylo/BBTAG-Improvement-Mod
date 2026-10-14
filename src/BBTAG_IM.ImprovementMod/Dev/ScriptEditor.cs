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

    public static void Open(string script, int line, string? program, ILog log)
    {
        var thread = new Thread(() => OpenNow(script, line, program, log)) { IsBackground = true, Name = ThreadName };

        if (OperatingSystem.IsWindows())
            thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
    }

    private static void OpenNow(string script, int line, string? program, ILog log)
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

            Start(program, ScriptEditorProgram.Arguments(program, script, line));
        }
        catch (Win32Exception exception)
        {
            SafeLog.Error(log, $"{Path.GetFileName(script)} could not be opened: {exception.Message}");
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
