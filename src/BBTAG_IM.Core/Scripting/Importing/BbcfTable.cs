using System.Globalization;
using System.Text.Json;

namespace BBTAG_IM.Core.Scripting.Importing;

public sealed record BbcfTable(IReadOnlyDictionary<int, BbcfCommand> Commands, IReadOnlyDictionary<int, string> Variables)
{
    private const string InstructionsField = "Instructions";
    private const string VariablesField = "Variables";
    private const string NameField = "Name";
    private const string ArgumentsField = "Args";
    private const string TypeField = "Type";

    public static BbcfTable Read(string json)
    {
        using var document = Parse(json);
        var root = document.RootElement;

        return new BbcfTable(ReadCommands(root), ReadVariables(root));
    }

    private static JsonDocument Parse(string json)
    {
        try
        {
            return JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException exception)
        {
            throw new InvalidScriptDatabaseException($"The BBCF table is not valid JSON: {exception.Message}");
        }
    }

    private static Dictionary<int, BbcfCommand> ReadCommands(JsonElement root)
    {
        var commands = new Dictionary<int, BbcfCommand>();

        if (!root.TryGetProperty(InstructionsField, out var instructions))
            return commands;

        foreach (var instruction in instructions.EnumerateObject())
        {
            if (Number(instruction.Name) is not { } id || Text(instruction.Value, NameField) is not { Length: > 0 } name)
                continue;

            commands[id] = new BbcfCommand(name, ArgumentTypes(instruction.Value));
        }

        return commands;
    }

    private static string[] ArgumentTypes(JsonElement instruction) =>
        instruction.TryGetProperty(ArgumentsField, out var arguments)
            ? arguments.EnumerateArray().Select(argument => Text(argument, TypeField) ?? string.Empty).ToArray()
            : [];

    private static Dictionary<int, string> ReadVariables(JsonElement root)
    {
        var variables = new Dictionary<int, string>();

        if (!root.TryGetProperty(VariablesField, out var element))
            return variables;

        foreach (var variable in element.EnumerateObject())
        {
            if (variable.Value.ValueKind == JsonValueKind.Number && variable.Value.TryGetInt32(out var slot))
                variables[slot] = variable.Name;
        }

        return variables;
    }

    private static string? Text(JsonElement element, string field) =>
        element.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int? Number(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;
}
