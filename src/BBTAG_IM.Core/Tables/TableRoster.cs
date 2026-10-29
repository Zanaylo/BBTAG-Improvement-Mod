using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace BBTAG_IM.Core.Tables;

public static partial class TableRoster
{
    private const string EntryElement = "CheatEntry";
    private const string DescriptionElement = "Description";
    private const string ScriptElement = "AssemblerScript";
    private const string RosterScript = "Characters";
    private const string PlaceholderCode = "temp";
    private const char DescriptionQuote = '"';

    public static IReadOnlyList<string> Codes(string xml) =>
        Load(xml).Descendants(EntryElement)
            .Where(IsRosterScript)
            .SelectMany(entry => CharacterId().Matches(entry.Element(ScriptElement)!.Value))
            .Select(match => match.Groups["code"].Value)
            .Where(code => code != PlaceholderCode)
            .Distinct()
            .ToArray();

    private static bool IsRosterScript(XElement entry) =>
        entry.Element(ScriptElement) is not null
        && entry.Element(DescriptionElement)?.Value.Trim().Trim(DescriptionQuote) == RosterScript;

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

    [GeneratedRegex(@"\bID\s*=\s*""(?<code>[a-z0-9]+)""")]
    private static partial Regex CharacterId();
}
