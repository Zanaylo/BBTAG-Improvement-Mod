namespace BBTAG_IM.Core.Mods;

public static class CharacterFileMessages
{
    private const string Complete = "Every character in the roster has the files a match needs.";
    private const string Separator = ", ";

    public static IReadOnlyList<string> From(IReadOnlyList<CharacterFileReport> reports)
    {
        if (reports.Count == 0)
            return [Complete];

        var absent = reports.Where(report => report.HasNoFiles).Select(report => report.Code).ToArray();
        var incomplete = reports.Where(report => !report.HasNoFiles).Select(report => report.Message);
        return absent.Length == 0 ? incomplete.ToArray() : [.. incomplete, Absent(absent)];
    }

    private static string Absent(IReadOnlyList<string> codes) =>
        codes.Count == 1
            ? $"1 character in the roster has no files at all. Picking it will crash the game: {codes[0]}."
            : $"{codes.Count} characters in the roster have no files at all. Picking one will crash the game: {string.Join(Separator, codes)}.";
}
