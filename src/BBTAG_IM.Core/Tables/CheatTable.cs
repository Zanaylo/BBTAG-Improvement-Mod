using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace BBTAG_IM.Core.Tables;

public sealed partial class CheatTable
{
    private const string RootElement = "CheatTable";
    private const string EntriesElement = "CheatEntries";
    private const string EntryElement = "CheatEntry";
    private const string IdElement = "ID";
    private const string DescriptionElement = "Description";
    private const string VariableTypeElement = "VariableType";
    private const string ScriptVariableType = "Auto Assembler Script";
    private const char DescriptionQuote = '"';
    private const string SingleSpace = " ";

    private readonly HashSet<int> _toggleableIds;
    private readonly int[] _requiredIds;
    private readonly Dictionary<int, string> _descriptions;

    private CheatTable(IReadOnlyList<TableScript> scripts)
    {
        Scripts = scripts;
        _toggleableIds = Flatten(scripts).Where(script => script.CanToggle).Select(script => script.Id).ToHashSet();
        _requiredIds = scripts.Where(IsCore).Select(script => script.Id).ToArray();
        _descriptions = Flatten(scripts).DistinctBy(script => script.Id).ToDictionary(script => script.Id, script => script.Description);
    }

    public IReadOnlyList<TableScript> Scripts { get; }

    public bool ContainsScript(int id) => _toggleableIds.Contains(id);

    public bool IsRequired(int id) => _requiredIds.Contains(id);

    public string Describe(int id) => _descriptions.GetValueOrDefault(id) ?? $"script {id}";

    public IReadOnlyList<int> SelectionToEnable(IReadOnlyList<int>? savedSelection) =>
        _requiredIds.Concat((savedSelection ?? []).Where(ContainsScript)).Distinct().ToArray();

    public static CheatTable Parse(string xml)
    {
        var root = Load(xml).Root;

        if (root?.Name.LocalName != RootElement)
            throw new InvalidCheatTableException("The file is not a Cheat Engine table.");

        var scripts = root.Element(EntriesElement)?.Elements(EntryElement).Select(ToScript).OfType<TableScript>().ToArray() ?? [];
        return new CheatTable(scripts);
    }

    private static XDocument Load(string xml)
    {
        try
        {
            return XDocument.Parse(xml);
        }
        catch (XmlException exception)
        {
            throw new InvalidCheatTableException($"The table could not be read: {exception.Message}", exception);
        }
    }

    private static TableScript? ToScript(XElement entry)
    {
        if (!int.TryParse(entry.Element(IdElement)?.Value, out var id))
            return null;

        var canToggle = entry.Element(VariableTypeElement)?.Value == ScriptVariableType;
        var children = entry.Element(EntriesElement)?.Elements(EntryElement).Select(ToScript).OfType<TableScript>().ToArray() ?? [];

        if (!canToggle && children.Length == 0)
            return null;

        return new TableScript(id, Clean(entry.Element(DescriptionElement)?.Value ?? ""), canToggle, children);
    }

    private static string Clean(string description) =>
        Whitespace().Replace(description.Trim().Trim(DescriptionQuote).Trim(), SingleSpace);

    private static bool IsCore(TableScript script) => script.CanToggle && CoreName().IsMatch(script.Description);

    private static IEnumerable<TableScript> Flatten(IEnumerable<TableScript> scripts) =>
        scripts.SelectMany(script => Flatten(script.Children).Prepend(script));

    [GeneratedRegex(@"^Core\b", RegexOptions.IgnoreCase)]
    private static partial Regex CoreName();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
