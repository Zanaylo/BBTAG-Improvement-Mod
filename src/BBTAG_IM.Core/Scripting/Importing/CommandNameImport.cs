using System.Text.Json.Nodes;

namespace BBTAG_IM.Core.Scripting.Importing;

public static class CommandNameImport
{
    private const string NameField = "name";
    private const string FormatField = "format";
    private const string SizeField = "size";

    public static NameImportResult Merge(string commandsJson, BbcfTable table, IReadOnlyDictionary<int, int> gameSizes)
    {
        var commands = DatabaseJson.Parse(commandsJson);
        var claims = new NameClaims(UsedNames(commands));
        var named = 0;
        var added = 0;

        foreach (var (id, command) in table.Commands.OrderBy(pair => pair.Key))
        {
            if (!gameSizes.TryGetValue(id, out var size) || TagCommandNames.Names.ContainsKey(id))
                continue;

            var key = DatabaseJson.Key(id);
            var entry = commands[key] as JsonObject;

            if (entry is not null && NameOf(entry) is not null)
                continue;

            if (!claims.TryClaim(command.Name, $"Command {key}"))
                continue;

            if (entry is null)
            {
                entry = NewEntry(command, size);
                commands[key] = entry;
                added++;
            }

            if (!HasLayout(entry))
                entry[SizeField] = size;

            entry[NameField] = command.Name;
            named++;
        }

        return new NameImportResult(DatabaseJson.WriteInNumberOrder(commands), named, added, claims.Skipped);
    }

    private static IEnumerable<string> UsedNames(JsonObject commands) =>
        commands.Select(entry => entry.Value as JsonObject)
            .Select(entry => entry is null ? null : NameOf(entry))
            .OfType<string>()
            .Concat(TagCommandNames.Names.Values);

    private static string? NameOf(JsonObject entry) =>
        entry[NameField] is JsonValue value && value.TryGetValue<string>(out var name) && name.Length > 0 && CommandDefinition.IdFromUnknownName(name) is null
            ? name
            : null;

    private static bool HasLayout(JsonObject entry) => entry.ContainsKey(FormatField) || entry.ContainsKey(SizeField);

    private static JsonObject NewEntry(BbcfCommand command, int size) =>
        BbcfFormat.For(command.ArgumentTypes, size) is { } format
            ? new JsonObject { [FormatField] = format }
            : new JsonObject { [SizeField] = size };
}
