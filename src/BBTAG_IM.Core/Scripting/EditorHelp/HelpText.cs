using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.Scripting.EditorHelp;

public static class HelpText
{
    private const string Paragraph = "\n\n";

    public static string Command(CommandDefinition command, CommandDoc? doc)
    {
        var text = new StringBuilder($"Command {Number(command.Id)}, {Number(command.Size)} bytes.");

        if (doc is null)
            text.Append(Paragraph).Append("Nothing is known about this command yet.");
        else
            AppendDoc(text, doc);

        return text.ToString();
    }

    public static string Value(int value, bool hex, string? description)
    {
        var number = hex ? $"{Hex(value)} ({Number(value)})" : Number(value);
        return $"{number}: {(string.IsNullOrEmpty(description) ? "No description yet." : description)}";
    }

    public static string Upon(int upon, ScriptDocs docs) => $"Upon {Number(upon)}{Explained(docs.Upon(upon))}";

    public static string Slot(int slot, ScriptDocs docs) => $"SLOT {Number(slot)}{Explained(docs.Slot(slot))}";

    public static string Hex(int value) => "0x" + value.ToString("x", CultureInfo.InvariantCulture);

    public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static void AppendDoc(StringBuilder text, CommandDoc doc)
    {
        if (doc.Summary.Length > 0)
            text.Append(Paragraph).Append(doc.Summary);

        if (doc.Arguments.Length > 0)
            text.Append(Paragraph).Append("Arguments: ").Append(doc.Arguments);

        if (doc.Notes.Length > 0)
            text.Append(Paragraph).Append(doc.Notes);
    }

    private static string Explained(string? doc) => doc is null ? "." : ": " + doc;
}
