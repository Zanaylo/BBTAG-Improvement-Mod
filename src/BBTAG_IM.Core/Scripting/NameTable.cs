namespace BBTAG_IM.Core.Scripting;

public sealed class NameTable
{
    public static readonly NameTable Empty = Of([]);

    private readonly Dictionary<int, string> names;
    private readonly Dictionary<int, string> descriptions;
    private readonly Dictionary<string, int> values;

    public NameTable(IEnumerable<KeyValuePair<int, string>> entries)
        : this(entries.Select(entry => new NamedValue(entry.Key, entry.Value)))
    {
    }

    private NameTable(IEnumerable<NamedValue> entries)
    {
        names = [];
        descriptions = [];
        values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (value, name, description) in entries)
        {
            names[value] = name;

            if (description.Length > 0)
                descriptions[value] = description;
            else
                descriptions.Remove(value);

            if (!values.TryAdd(name, value) && values[name] != value)
                ambiguous.Add(name);
        }

        foreach (var name in ambiguous)
            values.Remove(name);
    }

    public IEnumerable<NamedValue> Named =>
        names.Keys.Order().Where(value => NameOf(value) is not null).Select(Entry);

    public static NameTable Of(IEnumerable<NamedValue> entries) => new(entries);

    public string? NameOf(int value) =>
        names.TryGetValue(value, out var name) && values.TryGetValue(name, out var back) && back == value ? name : null;

    public int? ValueOf(string name) => values.TryGetValue(name, out var value) ? value : null;

    public string? DescriptionOf(int value) => descriptions.GetValueOrDefault(value);

    public NameTable With(NameTable overrides) =>
        new(Entries().Concat(overrides.Entries()).GroupBy(entry => entry.Value).Select(group => group.Last()));

    private IEnumerable<NamedValue> Entries() => names.Keys.Select(Entry);

    private NamedValue Entry(int value) => new(value, names[value], descriptions.GetValueOrDefault(value, string.Empty));
}
