using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Porting;

public sealed partial class SpriteRemap(
    IReadOnlyDictionary<int, int[]> durations,
    bool keepOtherSprites,
    string spritePrefix,
    IReadOnlyDictionary<int, int>? aliases = null)
{
    public const string NativePrefix = "Action_";

    private const string FunctionStart = "def ";
    private const string DecoratorStart = "@";
    private const char LineBreak = '\n';
    private const int FallbackPattern = 0;
    private const int FallbackFrame = 0;

    private static readonly HashSet<string> Markers = ["null", "keep"];

    public static string FrameName(string prefix, int pattern, int frame) => $"{prefix}{pattern:000}_{frame:00}";

    public string Apply(string script)
    {
        List<string> output = [];
        List<string> function = [];

        foreach (var line in script.Split(LineBreak))
        {
            if (line.StartsWith(FunctionStart, StringComparison.Ordinal) || line.StartsWith(DecoratorStart, StringComparison.Ordinal))
            {
                output.AddRange(ApplyToFunction(function));
                function.Clear();
            }

            function.Add(line);
        }

        output.AddRange(ApplyToFunction(function));
        return string.Join(LineBreak, output);
    }

    [GeneratedRegex(@"^(\s*)sprite\('([^']*)',\s*(-?\d+)\)\s*$")]
    private static partial Regex SpriteLine();

    [GeneratedRegex(@"^Action_(\d+)_(\d+)$")]
    private static partial Regex ActionSprite();

    [GeneratedRegex(@"^(\s*HitSprites\(\d+,\s*')([^']*)('\)\s*)$")]
    private static partial Regex HitSpriteLine();

    private int Target(int pattern) => aliases?.GetValueOrDefault(pattern, pattern) ?? pattern;

    private string HitSpriteName(string name)
    {
        if (ActionSprite().Match(name) is not { Success: true } action)
            return name;

        var pattern = Target(int.Parse(action.Groups[1].Value));

        if (!durations.TryGetValue(pattern, out var frames))
            return Fallback;

        return FrameName(spritePrefix, pattern, Math.Min(int.Parse(action.Groups[2].Value), frames.Length - 1));
    }

    private static string SpriteText(string indent, string name, int duration) => $"{indent}sprite('{name}', {duration})";

    private string Fallback => FrameName(spritePrefix, FallbackPattern, FallbackFrame);

    private List<string> ApplyToFunction(List<string> lines)
    {
        var uses = Uses(lines);
        List<string> output = [];

        for (var index = 0; index < lines.Count; index++)
        {
            var sprite = SpriteLine().Match(lines[index]);

            if (!sprite.Success)
            {
                output.Add(HitSpriteLine().Replace(lines[index], hit => hit.Groups[1].Value + HitSpriteName(hit.Groups[2].Value) + hit.Groups[3].Value));
                continue;
            }

            Rewrite(sprite, index, uses, output);
        }

        return output;
    }

    private static Dictionary<int, PatternUse> Uses(List<string> lines)
    {
        Dictionary<int, PatternUse> uses = [];

        for (var index = 0; index < lines.Count; index++)
        {
            var sprite = SpriteLine().Match(lines[index]);

            if (!sprite.Success || ActionSprite().Match(sprite.Groups[2].Value) is not { Success: true } action)
                continue;

            var pattern = int.Parse(action.Groups[1].Value);
            var frame = int.Parse(action.Groups[2].Value);
            var highest = uses.TryGetValue(pattern, out var known) ? Math.Max(known.HighestFrame, frame) : frame;
            uses[pattern] = new PatternUse(highest, index);
        }

        return uses;
    }

    private void Rewrite(Match sprite, int index, Dictionary<int, PatternUse> uses, List<string> output)
    {
        var indent = sprite.Groups[1].Value;
        var name = sprite.Groups[2].Value;
        var duration = int.Parse(sprite.Groups[3].Value);

        if (ActionSprite().Match(name) is not { Success: true } action)
        {
            output.Add(SpriteText(indent, OtherSpriteName(name), duration));
            return;
        }

        var pattern = int.Parse(action.Groups[1].Value);
        var target = Target(pattern);
        var frame = int.Parse(action.Groups[2].Value);

        if (!durations.TryGetValue(target, out var frames))
        {
            output.Add(SpriteText(indent, Fallback, duration));
            return;
        }

        var shown = Math.Min(frame, frames.Length - 1);
        output.Add(SpriteText(indent, FrameName(spritePrefix, target, shown), frames[shown]));

        if (uses[pattern].LastLine != index)
            return;

        for (var extra = uses[pattern].HighestFrame + 1; extra < frames.Length; extra++)
            output.Add(SpriteText(indent, FrameName(spritePrefix, target, extra), frames[extra]));
    }

    private string OtherSpriteName(string name) => keepOtherSprites || Markers.Contains(name) ? name : Fallback;

    private readonly record struct PatternUse(int HighestFrame, int LastLine);
}
