using System.Diagnostics;
using BBTAG_IM.Core.Debugging;
using BBTAG_IM.Launcher.Native;

namespace BBTAG_IM.Launcher.Debugging;

internal sealed class VisualStudioAttacher
{
    private const string ThreadName = "Visual Studio attach";
    private const int FirstItem = 1;

    public string? AttachTo(uint gameProcessId)
    {
        if (!Debugger.IsAttached)
            return null;

        var message = string.Empty;
        var thread = new Thread(() => message = Attach((int)gameProcessId)) { IsBackground = true, Name = ThreadName };
        thread.Start();
        thread.Join();
        return message;
    }

    private static string Attach(int gameProcessId)
    {
        var initialized = OleMethods.CoInitializeEx(0, OleMethods.MultiThreaded);

        try
        {
            using var table = RunningObjectTable.Open();
            var studios = table.ObjectsNamed(name => VisualStudioMoniker.ProcessIdOf(name) is not null);

            try
            {
                if (studios.FirstOrDefault(IsDebuggingThisProcess) is not { } studio)
                    return "No Visual Studio is debugging the Mod Loader, so the game runs without a debugger.";

                AttachDebugger(studio, gameProcessId);
                return "Visual Studio is debugging the game too, so breakpoints in the payload and the Improvement Mod are hit.";
            }
            finally
            {
                foreach (var each in studios)
                    each.Dispose();
            }
        }
        catch (AutomationException exception)
        {
            return $"Visual Studio could not attach to the game. {exception.Message}";
        }
        finally
        {
            if (initialized >= 0)
                OleMethods.CoUninitialize();
        }
    }

    private static bool IsDebuggingThisProcess(DispatchObject studio)
    {
        using var debugger = studio.Object("Debugger");
        using var debugged = debugger.Object("DebuggedProcesses");
        using var launcher = FindProcess(debugged, Environment.ProcessId);
        return launcher is not null;
    }

    private static void AttachDebugger(DispatchObject studio, int gameProcessId)
    {
        using var debugger = studio.Object("Debugger");
        using var local = debugger.Object("LocalProcesses");
        using var game = FindProcess(local, gameProcessId)
            ?? throw new AutomationException("The game is not in the Visual Studio process list.");

        game.Call("Attach");
    }

    private static DispatchObject? FindProcess(DispatchObject processes, int processId)
    {
        var count = processes.Integer("Count");

        for (var index = FirstItem; index <= count; index++)
        {
            var process = processes.Item(index);

            if (process.Integer("ProcessID") == processId)
                return process;

            process.Dispose();
        }

        return null;
    }
}
