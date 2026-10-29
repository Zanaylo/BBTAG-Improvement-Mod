namespace BBTAG_IM.Core.Scripting.Dev;

public static class ScriptEditorProgram
{
    public const string VisualStudioProcess = "devenv";

    private const string VisualStudio = "devenv.exe";
    private const string VisualStudioCode = "Code.exe";

    public static bool IsVisualStudio(string program) => Path.GetFileName(program).Equals(VisualStudio, StringComparison.OrdinalIgnoreCase);

    public static string Arguments(string program, string script, int line, string? project = null)
    {
        if (IsVisualStudio(program))
            return $"/edit \"{script}\"";

        if (Path.GetFileName(program).Equals(VisualStudioCode, StringComparison.OrdinalIgnoreCase))
            return project is null ? $"-g \"{script}:{line}\"" : $"\"{Path.GetDirectoryName(project)}\" -g \"{script}:{line}\"";

        return $"\"{script}\"";
    }

    public static string? ProjectArguments(string program, string? project, bool programRunning)
    {
        if (project is null || programRunning || !IsVisualStudio(program))
            return null;

        return $"\"{project}\"";
    }
}
