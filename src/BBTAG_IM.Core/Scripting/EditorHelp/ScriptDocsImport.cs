using System.Globalization;
using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Scripting.EditorHelp;

public static partial class ScriptDocsImport
{
    private const string FormatHeader = "Format";
    private const string FunctionHeader = "Function";
    private const string NotesHeader = "Notes";
    private const string TagMark = "TAG";
    private const string BBCFRowMark = "(CF)";
    private const string BBCFTestedSheet = "Command List";
    private const string SlotSheetPrefix = "SLOT";
    private const string EventSheetPrefix = "Events";
    private const string Todo = "[TODO]";
    private const int IdColumn = 0;
    private const int NameColumn = 1;
    private const int NotesColumn = 2;
    private const int TagRowRank = 4;
    private const int TestedRank = 3;
    private const int OtherSheetRank = 2;
    private const int BBCFRowRank = 1;

    public static ScriptDocs FromWorkbook(IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> sheets)
    {
        var commands = new Dictionary<int, (CommandDoc Doc, int Rank)>();

        foreach (var (name, rows) in sheets)
            AddCommands(commands, name, rows);

        return new ScriptDocs(
            commands.ToDictionary(entry => entry.Key, entry => entry.Value.Doc),
            Values(sheets, SlotSheetPrefix),
            Values(sheets, EventSheetPrefix));
    }

    private static void AddCommands(Dictionary<int, (CommandDoc Doc, int Rank)> commands, string sheet, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var headerIndex = rows.ToList().FindIndex(row => row.Contains(FormatHeader) && row.Contains(FunctionHeader));

        if (headerIndex < 0)
            return;

        var header = rows[headerIndex];
        var format = IndexOf(header, FormatHeader);
        var function = IndexOf(header, FunctionHeader);
        var notes = IndexOf(header, NotesHeader);

        foreach (var row in rows.Skip(headerIndex + 1))
        {
            var idCell = Cell(row, format - 1);

            if (Id(idCell) is not { } id)
                continue;

            var rank = Rank(sheet, idCell);

            if (commands.TryGetValue(id, out var known) && known.Rank >= rank)
                continue;

            commands[id] = (new CommandDoc(Clean(Cell(row, function)), Clean(Cell(row, format)), Clean(Cell(row, notes))), rank);
        }
    }

    private static Dictionary<int, string> Values(IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> sheets, string prefix)
    {
        var values = new Dictionary<int, string>();
        var matching = sheets
            .Where(sheet => sheet.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(sheet => sheet.Key.Contains(TagMark, StringComparison.OrdinalIgnoreCase));

        foreach (var (_, rows) in matching)
        {
            foreach (var row in rows)
            {
                if (Id(Cell(row, IdColumn)) is { } id && Description(row) is { } description)
                    values[id] = description;
            }
        }

        return values;
    }

    private static string? Description(IReadOnlyList<string> row)
    {
        var name = Clean(Cell(row, NameColumn));
        var notes = Clean(Cell(row, NotesColumn));

        if (name.Length == 0 || name == Todo)
            return null;

        if (notes.Length == 0)
            return name;

        return name.EndsWith('.') ? $"{name} {notes}" : $"{name}. {notes}";
    }

    private static int Rank(string sheet, string idCell)
    {
        if (!sheet.Contains(TagMark, StringComparison.OrdinalIgnoreCase))
            return sheet.Equals(BBCFTestedSheet, StringComparison.OrdinalIgnoreCase) ? TestedRank : OtherSheetRank;

        return idCell.Contains(BBCFRowMark, StringComparison.OrdinalIgnoreCase) ? BBCFRowRank : TagRowRank;
    }

    private static int? Id(string cell)
    {
        var match = LeadingNumber().Match(cell);
        return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
    }

    private static int IndexOf(IReadOnlyList<string> header, string name)
    {
        for (var index = 0; index < header.Count; index++)
        {
            if (header[index].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return -1;
    }

    private static string Cell(IReadOnlyList<string> row, int index) => index >= 0 && index < row.Count ? row[index] : string.Empty;

    private static string Clean(string text) =>
        string.Join('\n', text.ReplaceLineEndings("\n").Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0));

    [GeneratedRegex(@"^\s*(\d+)(?:\.0+)?(?:\s|\(|$)")]
    private static partial Regex LeadingNumber();
}
