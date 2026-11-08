namespace BBTAG_IM.Core.Porting;

public static class Ha6Stack
{
    private const string DataSection = "[DataFile]";
    private const string SectionStart = "[";
    private const string FileKey = "File";
    private const string SharedTemplate = "basedata";
    private const char ValueSeparator = '=';

    public static IReadOnlyList<string> Files(string loadList)
    {
        List<string> files = [];
        var inDataSection = false;

        foreach (var line in loadList.Split('\n').Select(line => line.Trim()))
        {
            if (line.StartsWith(SectionStart, StringComparison.Ordinal))
                inDataSection = line == DataSection;
            else if (inDataSection && IsFileEntry(line))
                files.Add(line[(line.IndexOf(ValueSeparator) + 1)..].Trim());
        }

        return files;
    }

    private static bool IsFileEntry(string line) =>
        line.StartsWith(FileKey, StringComparison.Ordinal) && line.Length > FileKey.Length && char.IsAsciiDigit(line[FileKey.Length]);

    public static bool IsSharedTemplate(string file) =>
        Path.GetFileNameWithoutExtension(file).Equals(SharedTemplate, StringComparison.OrdinalIgnoreCase);
}
