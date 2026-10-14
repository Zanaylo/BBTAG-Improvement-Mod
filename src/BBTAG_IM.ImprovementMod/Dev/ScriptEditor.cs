using System.Diagnostics;

namespace BBTAG_IM.ImprovementMod.Dev;

internal static class ScriptEditor
{
    private const string VisualStudioCodeFolder = "Microsoft VS Code";
    private const string VisualStudioCodeExecutable = "Code.exe";
    private const string VisualStudioFinder = @"Microsoft Visual Studio\Installer\vswhere.exe";
    private const string Notepad = "notepad.exe";

    public static string Open(string path, int line)
    {
        if (VisualStudioCode() is { } code)
        {
            Start(code, $"-g \"{path}:{line}\"");
            return "VS Code";
        }

        if (VisualStudio() is { } studio)
        {
            Start(studio, $"/edit \"{path}\"");
            return "Visual Studio";
        }

        Start(Notepad, $"\"{path}\"");
        return "Notepad";
    }

    private static string? VisualStudioCode()
    {
        string[] candidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", VisualStudioCodeFolder, VisualStudioCodeExecutable),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), VisualStudioCodeFolder, VisualStudioCodeExecutable),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), VisualStudioCodeFolder, VisualStudioCodeExecutable),
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? VisualStudio()
    {
        var finder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), VisualStudioFinder);

        if (!File.Exists(finder))
            return null;

        using var process = Process.Start(new ProcessStartInfo(finder, "-latest -property productPath")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        });

        var studio = process?.StandardOutput.ReadToEnd().Trim();
        return File.Exists(studio) ? studio : null;
    }

    private static void Start(string program, string arguments) =>
        Process.Start(new ProcessStartInfo(program, arguments) { UseShellExecute = false, CreateNoWindow = true })?.Dispose();
}
