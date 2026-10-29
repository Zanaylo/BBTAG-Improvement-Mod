using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Logging;

public sealed class ErrorLogFollower(IFileSystem fileSystem, string path)
{
    private const char EntryStart = '[';
    private const string ErrorMarker = "] ERROR ";
    private const char LineEnd = '\n';
    private const char Space = ' ';

    private int _readUpTo;

    public IReadOnlyList<string> NewErrors()
    {
        if (!fileSystem.FileExists(path))
            return [];

        var text = fileSystem.ReadAllText(path);

        if (text.Length < _readUpTo)
            _readUpTo = 0;

        var end = text.LastIndexOf(LineEnd) + 1;

        if (end <= _readUpTo)
            return [];

        var fresh = text[_readUpTo..end];
        _readUpTo = end;
        return Entries(fresh).Where(entry => entry.Contains(ErrorMarker, StringComparison.Ordinal))
            .Select(entry => entry[(entry.IndexOf(ErrorMarker, StringComparison.Ordinal) + ErrorMarker.Length)..])
            .ToArray();
    }

    private static List<string> Entries(string text)
    {
        List<string> entries = [];

        foreach (var line in text.Split(LineEnd).Select(line => line.Trim()).Where(line => line.Length > 0))
        {
            if (line[0] == EntryStart || entries.Count == 0)
                entries.Add(line);
            else
                entries[^1] = entries[^1] + Space + line;
        }

        return entries;
    }
}
