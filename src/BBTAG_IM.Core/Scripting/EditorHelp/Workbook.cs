using System.IO.Compression;
using System.Xml.Linq;

namespace BBTAG_IM.Core.Scripting.EditorHelp;

public static class Workbook
{
    private const string WorkbookPart = "xl/workbook.xml";
    private const string RelationshipsPart = "xl/_rels/workbook.xml.rels";
    private const string SharedStringsPart = "xl/sharedStrings.xml";
    private const string PartsFolder = "xl/";
    private const string SharedStringType = "s";
    private const string InlineStringType = "inlineStr";
    private const int ColumnLetters = 26;

    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> Read(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var sharedStrings = SharedStrings(zip);
        var targets = Part(zip, RelationshipsPart).Root!.Elements(PackageRelationships + "Relationship")
            .ToDictionary(relationship => (string)relationship.Attribute("Id")!, relationship => (string)relationship.Attribute("Target")!);
        var sheets = new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>();

        foreach (var sheet in Part(zip, WorkbookPart).Root!.Element(Main + "sheets")!.Elements(Main + "sheet"))
        {
            var target = targets[(string)sheet.Attribute(Relationships + "id")!].TrimStart('/');
            var path = target.StartsWith(PartsFolder, StringComparison.Ordinal) ? target : PartsFolder + target;
            sheets[(string)sheet.Attribute("name")!] = Rows(Part(zip, path), sharedStrings);
        }

        return sheets;
    }

    private static List<string> SharedStrings(ZipArchive zip) =>
        zip.GetEntry(SharedStringsPart) is null
            ? []
            : Part(zip, SharedStringsPart).Root!.Elements(Main + "si").Select(Text).ToList();

    private static List<IReadOnlyList<string>> Rows(XDocument sheet, List<string> sharedStrings) =>
        sheet.Descendants(Main + "row").Select(row => (IReadOnlyList<string>)Cells(row, sharedStrings)).ToList();

    private static List<string> Cells(XElement row, List<string> sharedStrings)
    {
        var cells = new List<string>();

        foreach (var cell in row.Elements(Main + "c"))
        {
            var column = Column((string?)cell.Attribute("r"), cells.Count);

            while (cells.Count < column)
                cells.Add(string.Empty);

            cells.Add(Value(cell, sharedStrings));
        }

        return cells;
    }

    private static string Value(XElement cell, List<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        var value = (string?)cell.Element(Main + "v");

        if (type == InlineStringType)
            return Text(cell.Element(Main + "is")!);

        if (value is null)
            return string.Empty;

        return type == SharedStringType ? sharedStrings[int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)] : value;
    }

    private static string Text(XElement item) => string.Concat(item.Descendants(Main + "t").Select(text => text.Value));

    private static int Column(string? reference, int next)
    {
        if (reference is null)
            return next;

        var column = 0;

        foreach (var letter in reference.TakeWhile(char.IsAsciiLetterUpper))
            column = column * ColumnLetters + (letter - 'A' + 1);

        return column - 1;
    }

    private static XDocument Part(ZipArchive zip, string path)
    {
        var entry = zip.GetEntry(path) ?? throw new InvalidDataException($"{path} is missing, so this is not an Excel workbook.");
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }
}
