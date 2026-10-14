namespace BBTAG_IM.Core.Scripting;

public sealed class NameTable
{
    public static readonly NameTable Empty = new([]);

    private readonly Dictionary<int, string> names;
    private readonly Dictionary<string, int> values;

    public NameTable(IEnumerable<KeyValuePair<int, string>> entries)
    {
        names = [];
        values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (value, name) in entries)
        {
            names[value] = name;

            if (!values.TryAdd(name, value) && values[name] != value)
                ambiguous.Add(name);
        }

        foreach (var name in ambiguous)
            values.Remove(name);
    }

    public string? NameOf(int value) =>
        names.TryGetValue(value, out var name) && values.TryGetValue(name, out var back) && back == value ? name : null;

    public int? ValueOf(string name) => values.TryGetValue(name, out var value) ? value : null;

    public NameTable With(NameTable overrides) => new(Entries().Concat(overrides.Entries()).GroupBy(entry => entry.Key).Select(group => group.Last()));

    private IEnumerable<KeyValuePair<int, string>> Entries() => names;
}
