using System.Globalization;
using System.Text;
using System.Text.Json;

namespace BBTAG_IM.Core.Scripting.EditorHelp;

public enum UsageKind
{
    Command,
    NormalInput,
    MoveInput,
    Upon,
    HitAnimation,
    MoveCondition,
}

public readonly record struct UsageKey(UsageKind Kind, int Value)
{
    private const char Separator = ':';

    public override string ToString() => Kind + Separator.ToString() + Value.ToString(CultureInfo.InvariantCulture);

    public static UsageKey? Parse(string text)
    {
        var parts = text.Split(Separator);

        if (parts.Length != 2 || !Enum.TryParse<UsageKind>(parts[0], out var kind) || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return null;

        return new UsageKey(kind, value);
    }
}

public sealed class UsageEntry
{
    public int Count { get; internal set; }

    public Dictionary<string, int> Characters { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Samples { get; } = [];
}

public sealed class ScriptUsage
{
    public const string FileName = "script_usage.json";
    public const int SampleLimit = 20;

    private const int DescribedSamples = 6;
    private const int NormalInputCommand = 14001;
    private const int UponBlockCommand = 15;
    private const string EntriesProperty = "entries";
    private const string CountProperty = "count";
    private const string CharactersProperty = "characters";
    private const string SamplesProperty = "samples";

    public static readonly ScriptUsage Empty = new();

    private readonly Dictionary<UsageKey, UsageEntry> entries = [];

    public IReadOnlyDictionary<UsageKey, UsageEntry> Entries => entries;

    public UsageEntry? Find(UsageKey key) => entries.GetValueOrDefault(key);

    public void Add(string character, ScriptBinary script)
    {
        var function = string.Empty;
        string? registration = null;

        foreach (var instruction in script.Instructions)
        {
            if (ScriptCommands.StartsFunction(instruction.Id))
                function = instruction.TextAt(0);

            if (instruction.Id == ScriptCommands.StateRegister)
                registration = instruction.TextAt(0);

            var place = registration is null ? $"{character} {function}" : $"{character} {registration} (registration)";

            if (instruction.Id == ScriptCommands.EndStateRegister)
                registration = null;
            Count(new UsageKey(UsageKind.Command, instruction.Id), character, place);

            foreach (var key in ValueKeys(instruction))
                Count(key, character, place);
        }
    }

    public string? Describe(UsageKey key)
    {
        if (Find(key) is not { } entry)
            return null;

        var times = entry.Count == 1 ? "1 time" : $"{entry.Count.ToString(CultureInfo.InvariantCulture)} times";
        var characters = entry.Characters.Count == 1 ? "1 character" : $"{entry.Characters.Count.ToString(CultureInfo.InvariantCulture)} characters";
        return $"In the game's scripts: {times}, {characters}. For example {string.Join(", ", Varied(entry.Samples).Take(DescribedSamples))}.";
    }

    private static IEnumerable<string> Varied(IReadOnlyList<string> samples)
    {
        var firstOfEach = samples.GroupBy(CharacterOf).Select(group => group.First()).ToList();
        return firstOfEach.Concat(samples.Except(firstOfEach));
    }

    private static string CharacterOf(string sample) => sample.Split(' ')[0];

    public string Write()
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteStartObject(EntriesProperty);

            foreach (var (key, entry) in entries.OrderBy(pair => pair.Key.Kind).ThenBy(pair => pair.Key.Value))
                WriteEntry(writer, key, entry);

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static ScriptUsage Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var usage = new ScriptUsage();

            if (!document.RootElement.TryGetProperty(EntriesProperty, out var all))
                return usage;

            foreach (var property in all.EnumerateObject())
            {
                if (UsageKey.Parse(property.Name) is { } key)
                    usage.entries[key] = ReadEntry(property.Value);
            }

            return usage;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            throw new InvalidScriptDatabaseException($"{FileName} cannot be read: {exception.Message}");
        }
    }

    private void Count(UsageKey key, string character, string place)
    {
        if (!entries.TryGetValue(key, out var entry))
            entries[key] = entry = new UsageEntry();

        entry.Count++;
        entry.Characters[character] = entry.Characters.GetValueOrDefault(character) + 1;

        if (entry.Samples.Contains(place))
            return;

        if (entry.Samples.Count < SampleLimit)
        {
            entry.Samples.Add(place);
            return;
        }

        if (entry.Samples.Any(sample => CharacterOf(sample) == character))
            return;

        var repeated = entry.Samples.FindLastIndex(sample => entry.Samples.Count(other => CharacterOf(other) == CharacterOf(sample)) > 1);

        if (repeated >= 0)
            entry.Samples[repeated] = place;
    }

    private static IEnumerable<UsageKey> ValueKeys(ScriptInstruction instruction)
    {
        if (instruction.Id == UponBlockCommand)
        {
            yield return new UsageKey(UsageKind.Upon, instruction.Values[0].Number);
            yield break;
        }

        if (instruction.Command.HasSlotArguments)
            yield break;

        for (var index = 0; index < instruction.Values.Count; index++)
        {
            if (instruction.Values[index].IsText || Kind(instruction.Id, index) is not { } kind)
                continue;

            yield return new UsageKey(kind, instruction.Values[index].Number);
        }
    }

    private static UsageKind? Kind(int command, int index) => ValueKinds.Of(command, index) switch
    {
        ValueKind.Input => command == NormalInputCommand ? UsageKind.NormalInput : UsageKind.MoveInput,
        ValueKind.Upon => UsageKind.Upon,
        ValueKind.HitAnimation => UsageKind.HitAnimation,
        ValueKind.MoveCondition => UsageKind.MoveCondition,
        _ => null,
    };

    private static void WriteEntry(Utf8JsonWriter writer, UsageKey key, UsageEntry entry)
    {
        writer.WriteStartObject(key.ToString());
        writer.WriteNumber(CountProperty, entry.Count);
        writer.WriteStartObject(CharactersProperty);

        foreach (var (character, count) in entry.Characters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            writer.WriteNumber(character, count);

        writer.WriteEndObject();
        writer.WriteStartArray(SamplesProperty);

        foreach (var sample in entry.Samples)
            writer.WriteStringValue(sample);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static UsageEntry ReadEntry(JsonElement element)
    {
        var entry = new UsageEntry { Count = element.GetProperty(CountProperty).GetInt32() };

        foreach (var character in element.GetProperty(CharactersProperty).EnumerateObject())
            entry.Characters[character.Name] = character.Value.GetInt32();

        entry.Samples.AddRange(element.GetProperty(SamplesProperty).EnumerateArray().Select(sample => sample.GetString() ?? string.Empty));
        return entry;
    }
}
