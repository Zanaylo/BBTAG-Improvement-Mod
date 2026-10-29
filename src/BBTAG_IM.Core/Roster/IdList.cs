using System.Text;

namespace BBTAG_IM.Core.Roster;

public static class IdList
{
    private const string LineEnd = "\r\n";
    private const string SectionEnd = "/// End Of Text";
    private const int ByteOrderMarkLength = 2;

    public static byte[] Insert(byte[] file, IReadOnlyList<KeyValuePair<string, string>> entries)
    {
        var missing = entries.Where(entry => !Contains(file, entry.Key)).ToArray();

        if (missing.Length == 0)
            return file;

        var lines = Encoding.Unicode.GetBytes(string.Concat(missing.Select(entry => entry.Key + LineEnd + entry.Value + LineEnd)));
        var at = FirstSectionEnd(file);

        return [.. file.AsSpan(0, at), .. lines, .. file.AsSpan(at)];
    }

    private static int FirstSectionEnd(byte[] file)
    {
        var marker = file.AsSpan().IndexOf(Encoding.Unicode.GetBytes(LineEnd + SectionEnd));
        return marker < 0 ? file.Length : marker + Encoding.Unicode.GetByteCount(LineEnd);
    }

    private static bool Contains(byte[] file, string key) =>
        file.AsSpan().IndexOf(Encoding.Unicode.GetBytes(LineEnd + key + LineEnd)) >= 0
        || file.AsSpan(ByteOrderMarkLength).StartsWith(Encoding.Unicode.GetBytes(key + LineEnd));
}
