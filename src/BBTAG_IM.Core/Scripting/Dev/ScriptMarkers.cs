namespace BBTAG_IM.Core.Scripting.Dev;

public static class ScriptMarkers
{
    public const string Prefix = "# !!! ";

    private const string UnixNewLine = "\n";
    private const string WindowsNewLine = "\r\n";

    public static string Strip(string text)
    {
        var newLine = NewLineOf(text);
        var lines = text.Split(newLine);
        return string.Join(newLine, lines.Where(line => !IsMarker(line)));
    }

    public static string Insert(string text, int line, string message)
    {
        var newLine = NewLineOf(text);
        var lines = text.Split(newLine).ToList();
        var index = line >= 1 && line <= lines.Count ? line - 1 : 0;
        var target = lines[index];
        var indent = target[..(target.Length - target.TrimStart().Length)];

        lines.Insert(index, indent + Prefix + OneLine(message));
        return string.Join(newLine, lines);
    }

    private static bool IsMarker(string line) => line.TrimStart().StartsWith(Prefix, StringComparison.Ordinal);

    private static string OneLine(string message) =>
        string.Join(' ', message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string NewLineOf(string text) => text.Contains(WindowsNewLine, StringComparison.Ordinal) ? WindowsNewLine : UnixNewLine;
}
