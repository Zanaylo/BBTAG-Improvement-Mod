namespace BBTAG_IM.Core.Scripting.Dev;

public static class ScriptEditorProgram
{
    private const string VisualStudio = "devenv.exe";
    private const string VisualStudioCode = "Code.exe";
    private const string AutomationPrefix = "!VisualStudio.DTE.";

    public static bool IsVisualStudio(string program) => Path.GetFileName(program).Equals(VisualStudio, StringComparison.OrdinalIgnoreCase);

    public static string Arguments(string program, string script, int line, string? solution = null)
    {
        if (IsVisualStudio(program))
            return solution is null ? $"/edit \"{script}\"" : $"\"{solution}\"";

        if (Path.GetFileName(program).Equals(VisualStudioCode, StringComparison.OrdinalIgnoreCase))
            return solution is null ? $"-g \"{script}:{line}\"" : $"\"{Path.GetDirectoryName(solution)}\" -g \"{script}:{line}\"";

        return $"\"{script}\"";
    }

    public static bool IsAutomationName(string displayName) => displayName.StartsWith(AutomationPrefix, StringComparison.Ordinal);

    public static bool IsSolution(string? loaded, string solution) =>
        !string.IsNullOrEmpty(loaded) && Path.GetFullPath(loaded).Equals(Path.GetFullPath(solution), StringComparison.OrdinalIgnoreCase);
}
