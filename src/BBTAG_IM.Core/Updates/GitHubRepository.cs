using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Updates;

public sealed partial record GitHubRepository(string Owner, string Name)
{
    private const string PageHost = "https://github.com/";
    private const string RawHost = "https://raw.githubusercontent.com/";
    private const char PathSeparator = '/';

    public string Url => PageHost + this;

    public static GitHubRepository? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = Link().Match(text.Trim());
        return match.Success ? new GitHubRepository(match.Groups["owner"].Value, match.Groups["name"].Value) : null;
    }

    public Uri RawFile(string commit, string path)
    {
        var escaped = string.Join(PathSeparator, path.Split(PathSeparator).Select(Uri.EscapeDataString));
        return new Uri($"{RawHost}{this}/{commit}/{escaped}");
    }

    public bool IsSame(GitHubRepository other) =>
        string.Equals(Owner, other.Owner, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => $"{Owner}/{Name}";

    [GeneratedRegex(@"^(?:(?:https?://)?(?:www\.)?github\.com/)?(?<owner>[A-Za-z0-9-]+)/(?<name>[A-Za-z0-9._-]+?)(?:\.git)?(?:/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex Link();
}
