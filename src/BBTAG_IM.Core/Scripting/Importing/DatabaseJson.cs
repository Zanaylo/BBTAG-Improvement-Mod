using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BBTAG_IM.Core.Scripting.Importing;

internal static class DatabaseJson
{
    private static readonly JsonDocumentOptions ReadOptions = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
    private static readonly JsonWriterOptions WriteOptions = new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static JsonObject Parse(string json)
    {
        try
        {
            return JsonNode.Parse(json, documentOptions: ReadOptions) as JsonObject
                ?? throw new InvalidScriptDatabaseException("A bbscript database file must hold a JSON object.");
        }
        catch (JsonException exception)
        {
            throw new InvalidScriptDatabaseException($"A bbscript database file is not valid JSON: {exception.Message}");
        }
    }

    public static string Key(int number) => number.ToString(CultureInfo.InvariantCulture);

    public static string WriteInNumberOrder(JsonObject json)
    {
        var entries = json.OrderBy(entry => NumberOf(entry.Key)).ToList();
        json.Clear();

        foreach (var (key, value) in entries)
            json[key] = value;

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, WriteOptions))
            json.WriteTo(writer);

        return Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine;
    }

    private static long NumberOf(string key) =>
        int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : long.MaxValue;
}
