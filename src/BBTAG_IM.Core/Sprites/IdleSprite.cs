using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Sprites;

public static partial class IdleSprite
{
    private const string IdleSuffix = "000_00.hip";

    public static FpacEntry? Pick(IEnumerable<FpacEntry> entries)
    {
        var sprites = entries.ToArray();
        var prefix = sprites
            .Select(entry => SpriteName().Match(entry.Name))
            .Where(match => match.Success)
            .GroupBy(match => match.Groups[1].Value, StringComparer.Ordinal)
            .MaxBy(group => group.Count())
            ?.Key;

        if (prefix is null)
            return null;

        var idle = prefix + IdleSuffix;
        return sprites.FirstOrDefault(entry => entry.Name.Equals(idle, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"^([A-Za-z]+)\d{3}_\d{2}\.hip$")]
    private static partial Regex SpriteName();
}
