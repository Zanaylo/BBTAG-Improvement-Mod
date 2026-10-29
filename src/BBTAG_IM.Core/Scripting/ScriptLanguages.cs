using BBTAG_IM.Core.Scripting.CSharp;

namespace BBTAG_IM.Core.Scripting;

public static class ScriptLanguages
{
    public const string PythonExtension = ".py";

    public static string Extension(ScriptLanguage language) => language == ScriptLanguage.CSharp ? CSharpProject.SourceExtension : PythonExtension;

    public static ScriptLanguage Of(string path) =>
        Path.GetExtension(path).Equals(CSharpProject.SourceExtension, StringComparison.OrdinalIgnoreCase) ? ScriptLanguage.CSharp : ScriptLanguage.Python;

    public static ScriptDecompiler Decompiler(ScriptDatabase database, ScriptLanguage language, ScriptStyle style = ScriptStyle.Readable) =>
        language == ScriptLanguage.CSharp ? new ScriptDecompiler(database, new CSharpDialect(database)) : new ScriptDecompiler(database, style);
}
