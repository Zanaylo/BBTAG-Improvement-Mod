using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Porting;

public static partial class PatternAliases
{
    private const int TemplateDash = 45;
    private const int NativeDash = 43;
    private const int NativeMediumNormal = 2;
    private const int NativeHeavyNormal = 3;
    private const int NativeFinisher = 131;

    private static readonly Dictionary<int, int> CrushAnimations = new()
    {
        [9] = NativeHeavyNormal,
        [68] = NativeHeavyNormal,
        [138] = NativeHeavyNormal,
        [190] = NativeHeavyNormal,
        [192] = NativeMediumNormal,
        [222] = NativeFinisher,
    };

    public static IReadOnlyDictionary<int, int> Parse(string vectorTable)
    {
        Dictionary<int, int> aliases = new(CrushAnimations) { [TemplateDash] = NativeDash };

        foreach (Match definition in NotedPattern().Matches(vectorTable))
            aliases.TryAdd(int.Parse(definition.Groups[2].Value), int.Parse(definition.Groups[1].Value));

        return aliases;
    }

    [GeneratedRegex(@"const\s+def_VLP_\w+\s*=\s*(\d+);[ \t]*//[ \t]*(\d+)")]
    private static partial Regex NotedPattern();
}
