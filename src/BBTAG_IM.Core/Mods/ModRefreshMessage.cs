namespace BBTAG_IM.Core.Mods;

public static class ModRefreshMessage
{
    private const string Read = "The mods folder was read again.";
    private const string NoChange = "Nothing was added or removed.";
    private const string Separator = ", ";

    public static string From(IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        var added = after.Except(before, StringComparer.OrdinalIgnoreCase).ToArray();
        var removed = before.Except(after, StringComparer.OrdinalIgnoreCase).ToArray();

        if (added.Length == 0 && removed.Length == 0)
            return $"{Read} {NoChange}";

        string?[] parts = [Read, Listed("New", added), Listed("Gone", removed)];
        return string.Join(' ', parts.OfType<string>());
    }

    private static string? Listed(string label, IReadOnlyList<string> names) =>
        names.Count == 0 ? null : $"{label}: {string.Join(Separator, names)}.";
}
