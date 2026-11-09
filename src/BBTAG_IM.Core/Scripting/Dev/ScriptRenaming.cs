using BBTAG_IM.Core.Scripting.Compiling;

namespace BBTAG_IM.Core.Scripting.Dev;

public static class ScriptRenaming
{
    public static string Rewrite(string source, ScriptLanguage language, ScriptDatabase before, ScriptDatabase after, string scriptName, ScriptStyle pythonLayout = ScriptStyle.Readable)
    {
        var bytes = new ScriptCompiler(before, language).Compile(source).Bytes;
        return ScriptLanguages.Decompiler(after, language, pythonLayout).Decompile(ScriptBinary.Read(bytes, after), scriptName);
    }
}
