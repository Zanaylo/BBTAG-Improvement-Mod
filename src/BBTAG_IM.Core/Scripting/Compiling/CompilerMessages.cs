namespace BBTAG_IM.Core.Scripting.Compiling;

public static class CompilerMessages
{
    private const string ErrorCode = "BBS0001";

    public static string Error(string path, ScriptCompileException mistake) => $"{path}({mistake.Line}): error {ErrorCode}: {mistake.Message}";
}
