namespace BBTAG_IM.Core.Scripting;

public static class FunctionNames
{
    private const string DigitPrefix = "__";

    private static readonly (string Text, string Replacement)[] Replacements =
    [
        ("-", "__ds__"),
        ("@", "__at__"),
        ("?", "__qu__"),
        (" ", "__sp__"),
    ];

    public static string? ToIdentifier(string name)
    {
        var identifier = name;

        foreach (var (text, replacement) in Replacements)
            identifier = identifier.Replace(text, replacement, StringComparison.Ordinal);

        if (identifier.Length > 0 && char.IsAsciiDigit(identifier[0]))
            identifier = DigitPrefix + identifier;

        return IsIdentifier(identifier) && FromIdentifier(identifier) == name ? identifier : null;
    }

    public static string FromIdentifier(string identifier)
    {
        var name = identifier.Length > DigitPrefix.Length && identifier.StartsWith(DigitPrefix, StringComparison.Ordinal) && char.IsAsciiDigit(identifier[DigitPrefix.Length])
            ? identifier[DigitPrefix.Length..]
            : identifier;

        foreach (var (text, replacement) in Replacements)
            name = name.Replace(replacement, text, StringComparison.Ordinal);

        return name;
    }

    public static bool IsIdentifier(string text) =>
        text.Length > 0
        && (char.IsAsciiLetter(text[0]) || text[0] == '_')
        && text.All(letter => char.IsAsciiLetterOrDigit(letter) || letter == '_')
        && !ScriptKeywords.Contains(text);
}
