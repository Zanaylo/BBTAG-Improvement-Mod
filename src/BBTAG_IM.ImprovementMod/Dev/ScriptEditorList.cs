using System.Diagnostics;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.ImprovementMod.Frames;

namespace BBTAG_IM.ImprovementMod.Dev;

internal sealed class ScriptEditorList(ILog log)
{
    private const string ScriptExtension = ".py";
    private const string VisualStudioFinder = @"Microsoft Visual Studio\Installer\vswhere.exe";
    private const string ThreadName = "Script editor list";
    private const int Idle = 0;
    private const int Loading = 1;

    private IReadOnlyList<ScriptEditorChoice> _choices = [];
    private bool _loaded;
    private int _state;

    public IReadOnlyList<ScriptEditorChoice> Choices => Volatile.Read(ref _choices);

    public bool IsLoading => Volatile.Read(ref _state) == Loading;

    public void LoadOnce()
    {
        if (!Volatile.Read(ref _loaded))
            Refresh();
    }

    public void Refresh()
    {
        if (Interlocked.Exchange(ref _state, Loading) == Loading)
            return;

        var thread = new Thread(Load) { IsBackground = true, Name = ThreadName };

        if (OperatingSystem.IsWindows())
            thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
    }

    private void Load()
    {
        try
        {
            Volatile.Write(ref _choices, ScriptEditorChoices.Merge([.. VisualStudios(), .. FileTypePrograms.For(ScriptExtension)]));
        }
        catch (Exception exception)
        {
            SafeLog.Error(log, $"The list of programs that open scripts could not be read: {exception.Message}");
        }
        finally
        {
            Volatile.Write(ref _loaded, true);
            Volatile.Write(ref _state, Idle);
        }
    }

    private static IEnumerable<ScriptEditorChoice> VisualStudios()
    {
        var finder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), VisualStudioFinder);

        if (!File.Exists(finder))
            return [];

        var programs = Lines(finder, "-all -sort -property productPath");
        var names = Lines(finder, "-all -sort -property displayName");

        return programs.Zip(names, (program, name) => new ScriptEditorChoice(name, program)).Where(choice => File.Exists(choice.Program));
    }

    private static string[] Lines(string finder, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(finder, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        });

        return process?.StandardOutput.ReadToEnd().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }
}
