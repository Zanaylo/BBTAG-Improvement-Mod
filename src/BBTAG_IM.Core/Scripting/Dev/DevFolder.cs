namespace BBTAG_IM.Core.Scripting.Dev;

public sealed record DevFolder(string Path)
{
    public const string Name = "DEV";

    private const string DataFolder = "data";
    private const string CharacterFolder = "char";
    private const string EffectsSuffix = "ea";
    private const string BinaryExtension = ".bin";

    public static DevFolder ForMod(string modFolder)
    {
        var modsFolder = System.IO.Path.GetDirectoryName(System.IO.Path.TrimEndingDirectorySeparator(modFolder)) ?? modFolder;
        return new DevFolder(System.IO.Path.Combine(modsFolder, Name));
    }

    public static string ScriptArchiveName(string code) => $"char_{code}_scr";

    public static string ScriptFileName(string code, bool effects) => $"scr_{code}{(effects ? EffectsSuffix : string.Empty)}";

    public string ScriptArchiveFolder(string code) => System.IO.Path.Combine(Path, DataFolder, CharacterFolder, ScriptArchiveName(code));

    public ScriptPaths Script(string code, bool effects, ScriptLanguage language = ScriptLanguage.CSharp)
    {
        var file = System.IO.Path.Combine(ScriptArchiveFolder(code), ScriptFileName(code, effects));
        return new ScriptPaths(file + ScriptLanguages.Extension(language), file + BinaryExtension);
    }
}
