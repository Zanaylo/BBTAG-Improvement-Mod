using System.Globalization;
using System.Text.Json;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting.CSharp;
using BBTAG_IM.Core.Scripting.EditorHelp;

namespace BBTAG_IM.Core.Scripting;

public sealed class ScriptDatabaseFolder(IFileSystem fileSystem, string folder)
{
    public const string CommandsFile = "command_db.json";
    public const string EditorProjectFolder = "Scripts";
    public const string GameSizesFile = "game_command_sizes.tsv";

    private const string NamedValuesFolder = "named_values";
    private const string SlotsFolder = "slot_db";
    private const string UponsFolder = "upon_db";
    private const string GlobalFile = "global.json";
    private const string JsonExtension = ".json";

    public bool Exists => fileSystem.FileExists(CommandsPath);

    public CommandDatabaseChoice CommandChoice => new(fileSystem, folder);

    public string CommandsPath => CommandChoice.CurrentPath;

    public string GlobalSlotsPath => Path.Combine(folder, SlotsFolder, GlobalFile);

    public string DocsPath => Path.Combine(folder, ScriptDocsFile.FileName);

    public string UsagePath => Path.Combine(folder, ScriptUsage.FileName);

    public string EditorSolutionPath => Path.Combine(folder, EditorProjectFolder, CSharpProject.SolutionFileName);

    public string? ExistingEditorSolution => fileSystem.FileExists(EditorSolutionPath) ? EditorSolutionPath : null;

    public void WriteEditorApi(string api) => WriteWhenChanged(Path.Combine(folder, EditorProjectFolder, CSharpApi.FileName), api);

    public void WriteGameSizes(IReadOnlyDictionary<int, int> sizes) => WriteWhenChanged(GameSizesPath, CommandSizesText.Write(sizes));

    public IReadOnlyDictionary<int, int>? LoadGameSizes() =>
        fileSystem.FileExists(GameSizesPath) ? CommandSizesText.Read(fileSystem.ReadAllText(GameSizesPath)) : null;

    private string GameSizesPath => Path.Combine(folder, GameSizesFile);

    private void WriteWhenChanged(string path, string text)
    {
        if (fileSystem.FileExists(path) && fileSystem.ReadAllText(path) == text)
            return;

        fileSystem.WriteAllText(path, text);
    }

    public ScriptUsage LoadUsage() =>
        fileSystem.FileExists(UsagePath) ? ScriptUsage.Read(fileSystem.ReadAllText(UsagePath)) : ScriptUsage.Empty;

    public ScriptDocs LoadDocs() =>
        fileSystem.FileExists(DocsPath) ? ScriptDocsFile.Read(fileSystem.ReadAllText(DocsPath)) : ScriptDocs.Empty;

    public ScriptDatabase Load()
    {
        if (!Exists)
            throw new InvalidScriptDatabaseException($"{CommandsFile} was not found in {folder}.");

        return new ScriptDatabase(
            Commands(),
            Names(GlobalSlotsPath),
            Names(UponsFolder, GlobalFile),
            Names(NamedValuesFolder, "hit_animation.json"),
            Names(NamedValuesFolder, "move_condition.json"),
            Inputs());
    }

    public ScriptDatabase ForCharacter(ScriptDatabase database, string characterCode) =>
        database.WithCharacterNames(
            Names(SlotsFolder, characterCode + JsonExtension),
            Names(UponsFolder, characterCode + JsonExtension));

    private IEnumerable<CommandDefinition> Commands()
    {
        var chosen = Commands(CommandsPath);
        var defaultPath = CommandChoice.DefaultPath;

        if (string.Equals(CommandsPath, defaultPath, StringComparison.OrdinalIgnoreCase) || !fileSystem.FileExists(defaultPath))
            return chosen;

        var layouts = Commands(defaultPath).ToDictionary(command => command.Id);
        var named = chosen.Select(command => layouts.TryGetValue(command.Id, out var layout) ? layout with { Name = command.Name } : command).ToList();
        var chosenIds = chosen.Select(command => command.Id).ToHashSet();

        return named.Concat(layouts.Values.Where(layout => !chosenIds.Contains(layout.Id)).Select(layout => layout with { Name = CommandDefinition.UnknownName(layout.Id) }));
    }

    private List<CommandDefinition> Commands(string path)
    {
        using var document = Parse(path) ?? throw new InvalidScriptDatabaseException($"{Path.GetFileName(path)} cannot be read.");
        var commands = new List<CommandDefinition>();

        foreach (var entry in document.RootElement.EnumerateObject())
        {
            if (HasLayout(entry.Value))
                commands.Add(Command(entry));
        }

        return commands;
    }

    private static CommandDefinition Command(JsonProperty entry)
    {
        if (!int.TryParse(entry.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            throw new InvalidScriptDatabaseException($"\"{entry.Name}\" is not a command number.");

        var data = entry.Value;
        var name = data.TryGetProperty("name", out var nameValue) ? nameValue.GetString() : null;

        return new CommandDefinition(
            id,
            string.IsNullOrEmpty(name) ? CommandDefinition.UnknownName(id) : name,
            Arguments(data),
            SlotTypeArguments(data),
            Hex(data));
    }

    private static bool HasLayout(JsonElement data) => data.TryGetProperty("format", out _) || data.TryGetProperty("size", out _);

    private static IReadOnlyList<CommandArgument> Arguments(JsonElement data)
    {
        if (!data.TryGetProperty("format", out var format))
            return CommandFormat.ForSize(data.GetProperty("size").GetInt32());

        try
        {
            return CommandFormat.Parse(format.GetString() ?? string.Empty);
        }
        catch (InvalidScriptDatabaseException)
        {
            return data.TryGetProperty("size", out var size) ? CommandFormat.ForSize(size.GetInt32()) : [];
        }
    }

    private static HashSet<int> SlotTypeArguments(JsonElement data) =>
        data.TryGetProperty("type_check", out var typeCheck)
            ? typeCheck.EnumerateArray().Select(index => index.GetInt32()).ToHashSet()
            : [];

    private static HexArguments Hex(JsonElement data)
    {
        if (!data.TryGetProperty("hex", out var hex))
            return HexArguments.None;

        return hex.ValueKind switch
        {
            JsonValueKind.True => HexArguments.All,
            JsonValueKind.Array => HexArguments.Only(hex.EnumerateArray().Select(index => index.GetInt32())),
            _ => HexArguments.None,
        };
    }

    private InputNames Inputs()
    {
        var normal = Parse(Path.Combine(folder, NamedValuesFolder, "normal_inputs.json"));

        using (normal)
        {
            return new InputNames(
                Names(NamedValuesFolder, "move_inputs.json"),
                new ButtonByteInputs(Group(normal, "grouped_values"), Group(normal, "button_byte"), Group(normal, "direction_byte")));
        }
    }

    private NameTable Names(string subfolder, string file) => Names(Path.Combine(folder, subfolder, file));

    private NameTable Names(string path)
    {
        using var document = Parse(path);
        return document is null ? NameTable.Empty : Table(document.RootElement);
    }

    private static NameTable Group(JsonDocument? document, string group) =>
        document is not null && document.RootElement.TryGetProperty(group, out var element) ? Table(element) : NameTable.Empty;

    private static NameTable Table(JsonElement element)
    {
        var entries = new List<KeyValuePair<int, string>>();

        foreach (var entry in element.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.String
                && int.TryParse(entry.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                entries.Add(new KeyValuePair<int, string>(value, entry.Value.GetString()!));
        }

        return new NameTable(entries);
    }

    private JsonDocument? Parse(string path)
    {
        if (!fileSystem.FileExists(path))
            return null;

        try
        {
            return JsonDocument.Parse(fileSystem.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException exception)
        {
            throw new InvalidScriptDatabaseException($"{path} is not valid JSON: {exception.Message}");
        }
    }
}
