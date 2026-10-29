namespace BBTAG_IM.Core.Mods;

public sealed record CharacterFileReport(string Code, IReadOnlyList<string> Missing, int Required)
{
    private const int NamedFiles = 8;
    private const string Separator = ", ";

    public bool HasNoFiles => Missing.Count == Required;

    public string Message => $"{Code} lacks {Missing.Count} {(Missing.Count == 1 ? "file" : "files")} that every original character has. "
        + $"A match with it may crash. Missing: {MissingList()}.";

    private string MissingList()
    {
        var named = string.Join(Separator, Missing.Take(NamedFiles));
        var unnamed = Missing.Count - NamedFiles;
        return unnamed > 0 ? $"{named} and {unnamed} more" : named;
    }
}
