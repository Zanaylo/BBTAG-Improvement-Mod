using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.EditorHelp;

namespace BBTAG_IM.BBScript;

internal sealed class ScriptDatabases(ScriptDatabaseFolder folder, ScriptDatabase shared)
{
    private const string DefaultFolderName = "bbscript";

    public static ScriptDatabases Open(string? path, ScriptStyle style)
    {
        var folderPath = path ?? Path.Combine(AppContext.BaseDirectory, DefaultFolderName);
        var folder = new ScriptDatabaseFolder(new PhysicalFileSystem(), folderPath);

        if (path is not null && !folder.Exists)
            throw new InvalidScriptDatabaseException($"{ScriptDatabaseFolder.CommandsFile} was not found in {path}.");

        if (!folder.Exists)
            Console.Error.WriteLine($"{ScriptDatabaseFolder.CommandsFile} was not found in {folderPath}, so commands only have numbers (Unknown9000). Use --db to point at the bbscript folder.");

        var sizes = StockCommandSizes.Load();
        var loaded = (folder.Exists ? folder.Load() : ScriptDatabase.Unnamed).WithGameSizes(sizes);
        var shared = style == ScriptStyle.Readable ? loaded.WithTagNames() : loaded;

        return new ScriptDatabases(folder, shared);
    }

    public ScriptDatabase Shared => shared;

    public string DocsPath => folder.DocsPath;

    public ScriptDocs Docs() => folder.LoadDocs();

    public string UsagePath => folder.UsagePath;

    public ScriptUsage Usage() => folder.LoadUsage();

    public ScriptDatabase For(string scriptName) =>
        folder.Exists && ScriptArchive.CharacterCode(scriptName) is { } code ? folder.ForCharacter(shared, code) : shared;
}
