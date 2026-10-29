using System.IO.Compression;
using System.Security;
using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal static class WorkbookFile
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static byte[] Create(params (string Name, string?[][] Rows)[] sheets)
    {
        var sharedStrings = new List<string>();
        using var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var workbook = new StringBuilder($"<workbook xmlns=\"{Main}\" xmlns:r=\"{Relationships}\"><sheets>");
            var relationships = new StringBuilder($"<Relationships xmlns=\"{PackageRelationships}\">");

            for (var index = 0; index < sheets.Length; index++)
            {
                var number = index + 1;
                workbook.Append($"<sheet name=\"{SecurityElement.Escape(sheets[index].Name)}\" sheetId=\"{number}\" r:id=\"rId{number}\"/>");
                relationships.Append($"<Relationship Id=\"rId{number}\" Target=\"worksheets/sheet{number}.xml\"/>");
                Add(zip, $"xl/worksheets/sheet{number}.xml", Sheet(sheets[index].Rows, sharedStrings));
            }

            Add(zip, "xl/workbook.xml", workbook.Append("</sheets></workbook>").ToString());
            Add(zip, "xl/_rels/workbook.xml.rels", relationships.Append("</Relationships>").ToString());
            Add(zip, "xl/sharedStrings.xml", $"<sst xmlns=\"{Main}\">{string.Concat(sharedStrings.Select(text => $"<si><t>{SecurityElement.Escape(text)}</t></si>"))}</sst>");
        }

        return stream.ToArray();
    }

    private static string Sheet(string?[][] rows, List<string> sharedStrings)
    {
        var sheet = new StringBuilder($"<worksheet xmlns=\"{Main}\"><sheetData>");

        for (var row = 0; row < rows.Length; row++)
        {
            sheet.Append($"<row r=\"{row + 1}\">");

            for (var column = 0; column < rows[row].Length; column++)
            {
                if (rows[row][column] is not { } value)
                    continue;

                var reference = $"{(char)('A' + column)}{row + 1}";

                if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    sheet.Append($"<c r=\"{reference}\"><v>{value}</v></c>");
                    continue;
                }

                sharedStrings.Add(value);
                sheet.Append($"<c r=\"{reference}\" t=\"s\"><v>{sharedStrings.Count - 1}</v></c>");
            }

            sheet.Append("</row>");
        }

        return sheet.Append("</sheetData></worksheet>").ToString();
    }

    private static void Add(ZipArchive zip, string name, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
