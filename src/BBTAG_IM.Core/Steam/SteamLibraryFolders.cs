using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Steam;

public static partial class SteamLibraryFolders
{
    private const string EscapedBackslash = @"\\";
    private const string Backslash = @"\";
    private const string EscapedQuote = "\\\"";
    private const string Quote = "\"";

    public static IReadOnlyList<string> ParsePaths(string libraryFoldersVdf) =>
        PathEntry().Matches(libraryFoldersVdf).Select(match => Unescape(match.Groups["value"].Value)).ToArray();

    public static string? ParseInstallDirectory(string appManifestAcf)
    {
        var match = InstallDirectoryEntry().Match(appManifestAcf);
        return match.Success ? Unescape(match.Groups["value"].Value) : null;
    }

    private static string Unescape(string value) => value.Replace(EscapedQuote, Quote).Replace(EscapedBackslash, Backslash);

    [GeneratedRegex("\"path\"\\s+\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex PathEntry();

    [GeneratedRegex("\"installdir\"\\s+\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex InstallDirectoryEntry();
}
