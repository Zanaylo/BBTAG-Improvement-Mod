namespace BBTAG_IM.Core.Scripting.Dev;

public static class ScriptEditorProgram
{
    private const string VisualStudio = "devenv.exe";
    private const string VisualStudioCode = "Code.exe";

    public static string Arguments(string program, string script, int line) => Path.GetFileName(program) switch
    {
        var name when name.Equals(VisualStudio, StringComparison.OrdinalIgnoreCase) => $"/edit \"{script}\"",
        var name when name.Equals(VisualStudioCode, StringComparison.OrdinalIgnoreCase) => $"-g \"{script}:{line}\"",
        _ => $"\"{script}\"",
    };
}
