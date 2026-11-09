using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Updates;

public static partial class ReleaseVersion
{
    private const int MissingPart = 0;

    public static Version? FromTag(string tag)
    {
        var match = Numbers().Match(tag);
        return match.Success && Version.TryParse(match.Value, out var version) ? version : null;
    }

    public static bool IsNewer(Version release, Version installed) => Filled(release) > Filled(installed);

    public static string Text(Version version) => version.Revision > 0 ? version.ToString(4) : Filled(version).ToString(3);

    private static Version Filled(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, MissingPart), Math.Max(version.Revision, MissingPart));

    [GeneratedRegex(@"\d+(?:\.\d+){1,3}")]
    private static partial Regex Numbers();
}
