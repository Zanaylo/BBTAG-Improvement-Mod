namespace BBTAG_IM.Core.Scripting.Compiling;

public sealed class ScriptCompileException(string message, int line) : Exception(message)
{
    public int Line { get; } = line;
}
