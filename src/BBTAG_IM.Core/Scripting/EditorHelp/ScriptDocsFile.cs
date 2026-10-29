using System.Globalization;
using System.Text;
using System.Text.Json;

namespace BBTAG_IM.Core.Scripting.EditorHelp;

public static class ScriptDocsFile
{
    public const string FileName = "command_docs.json";

    private const string CommandsProperty = "commands";
    private const string SlotsProperty = "slots";
    private const string UponsProperty = "upons";
    private const string SummaryProperty = "summary";
    private const string ArgumentsProperty = "arguments";
    private const string NotesProperty = "notes";

    public static string Write(ScriptDocs docs)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteStartObject(CommandsProperty);

            foreach (var (id, doc) in docs.Commands.OrderBy(entry => entry.Key))
            {
                writer.WriteStartObject(Key(id));
                writer.WriteString(SummaryProperty, doc.Summary);
                writer.WriteString(ArgumentsProperty, doc.Arguments);
                writer.WriteString(NotesProperty, doc.Notes);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            WriteValues(writer, SlotsProperty, docs.Slots);
            WriteValues(writer, UponsProperty, docs.Upons);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static ScriptDocs Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var root = document.RootElement;

            return new ScriptDocs(Commands(root), Values(root, SlotsProperty), Values(root, UponsProperty));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            throw new InvalidScriptDatabaseException($"{FileName} cannot be read: {exception.Message}");
        }
    }

    private static Dictionary<int, CommandDoc> Commands(JsonElement root)
    {
        var commands = new Dictionary<int, CommandDoc>();

        if (!root.TryGetProperty(CommandsProperty, out var element))
            return commands;

        foreach (var entry in element.EnumerateObject())
        {
            commands[Id(entry.Name)] = new CommandDoc(
                Text(entry.Value, SummaryProperty),
                Text(entry.Value, ArgumentsProperty),
                Text(entry.Value, NotesProperty));
        }

        return commands;
    }

    private static Dictionary<int, string> Values(JsonElement root, string property)
    {
        var values = new Dictionary<int, string>();

        if (!root.TryGetProperty(property, out var element))
            return values;

        foreach (var entry in element.EnumerateObject())
            values[Id(entry.Name)] = entry.Value.GetString() ?? string.Empty;

        return values;
    }

    private static void WriteValues(Utf8JsonWriter writer, string property, IReadOnlyDictionary<int, string> values)
    {
        writer.WriteStartObject(property);

        foreach (var (id, text) in values.OrderBy(entry => entry.Key))
            writer.WriteString(Key(id), text);

        writer.WriteEndObject();
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static int Id(string key) => int.Parse(key, CultureInfo.InvariantCulture);

    private static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);
}
