using System.Globalization;
using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Porting.Moves;

public sealed partial class CharacterStats
{
    private const string WalkVariable = "walk_vectorar";
    private const string JumpVariable = "jump_vectorar";
    private const string HighJumpVariable = "highjump_vectorar";
    private const string DashVariable = "dash_f_vector_tbl";
    private const double StandardHealth = 16000;
    private const double NativeStandardHealth = 11000;
    private const int HealthStep = 1000;
    private const int CharacterNumberStart = 3;
    private const int WalkForward = 1;
    private const int WalkBack = 3;
    private const int JumpUp = 0;
    private const int JumpGravity = 1;
    private const int JumpForward = 2;
    private const int JumpBack = 4;

    private CharacterStats(IReadOnlyList<string> lines, double damageScale)
    {
        Lines = lines;
        DamageScale = damageScale;
    }

    public IReadOnlyList<string> Lines { get; }

    public double DamageScale { get; }

    public static CharacterStats Parse(string moveTable, string status, string standardStatus, string character, string characterNames = "")
    {
        var chrNo = NameOf(characterNames, character) ?? "Def_ChrNo_" + char.ToUpperInvariant(character[0]) + character[1..];
        var table = new ParameterTable(moveTable, chrNo);
        List<string> lines = [];

        if (table.Numbers(WalkVariable) is { Count: > WalkBack } walk)
        {
            lines.Add(Line("WalkFSpeed", walk[WalkForward]));
            lines.Add(Line("WalkBSpeed", -walk[WalkBack]));
        }

        if (table.Numbers(JumpVariable) is { Count: > JumpBack } jump)
        {
            lines.Add(Line("JumpYVelocity", -jump[JumpUp]));
            lines.Add(Line("Gravity", jump[JumpGravity]));
            lines.Add(Line("InitFJumpXSpeed", jump[JumpForward]));
            lines.Add(Line("InitBJumpXSpeed", -jump[JumpBack]));
        }

        if (table.Numbers(HighJumpVariable) is { Count: > JumpBack } high)
        {
            lines.Add(Line("SuperJumpYVelocity", -high[JumpUp]));
            lines.Add(Line("InitFHighJumpXSpeed", high[JumpForward]));
            lines.Add(Line("InitBHighJumpXSpeed", -high[JumpBack]));
        }

        lines.AddRange(DashLines(table.Text(DashVariable)));

        var native = HealthOf(status, last: true);
        var scale = StandardHealth / (HealthOf(standardStatus, last: false) ?? NativeStandardHealth);

        if (native is { } health)
            lines.Add($"Health({(int)Math.Round(health * scale / HealthStep) * HealthStep})");

        return new CharacterStats(lines, scale);
    }

    private static string? NameOf(string characterNames, string character)
    {
        var number = int.Parse(character.AsSpan(CharacterNumberStart), CultureInfo.InvariantCulture);
        return CharacterName().Matches(characterNames).FirstOrDefault(match => int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) == number)?.Groups[1].Value;
    }

    [GeneratedRegex(@"const\s+(Def_ChrNo_\w+)\s*=\s*(\d+)\s*;")]
    private static partial Regex CharacterName();

    [GeneratedRegex(@"HpDivValue\s*=\s*\[\s*([\d\s,]+?)\s*\]")]
    private static partial Regex Health();

    [GeneratedRegex(@"\bx\s*=\s*(-?\d+)\s*,\s*addx\s*=\s*(-?\d+)(?:\s*,\s*max\s*=\s*(-?\d+))?")]
    private static partial Regex DashStep();

    private static string Line(string command, int hanteiSpeed) => $"{command}({NativeUnits.Speed(hanteiSpeed)})";

    private static IEnumerable<string> DashLines(string? table)
    {
        if (table is null)
            yield break;

        var steps = DashStep().Matches(table);

        if (steps.Count == 0)
            yield break;

        var last = steps[^1];
        yield return Line("DashFInitialVelocity", int.Parse(last.Groups[1].Value, CultureInfo.InvariantCulture));
        yield return Line("DashFAccel", int.Parse(last.Groups[2].Value, CultureInfo.InvariantCulture));

        if (last.Groups[3].Success)
            yield return Line("DashFMaxVelocity", int.Parse(last.Groups[3].Value, CultureInfo.InvariantCulture));
    }

    private static double? HealthOf(string status, bool last)
    {
        var matches = Health().Matches(status);

        if (matches.Count == 0)
            return null;

        var value = matches[last ? ^1 : 0].Groups[1].Value;
        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Sum(part => double.Parse(part, CultureInfo.InvariantCulture));
    }

    private sealed partial class ParameterTable(string text, string chrNo)
    {
        public IReadOnlyList<int>? Numbers(string variable) =>
            Text(variable) is { } value ? NumberPattern().Matches(value).Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray() : null;

        public string? Text(string variable)
        {
            var use = new Regex($@"local\s+{variable}\s*=\s*chrparam\.Get\(\s*\{{\s*type\s*=\s*""([^""]*)""").Match(text);

            if (!use.Success)
                return null;

            var start = text.IndexOf($"local type = \"{use.Groups[1].Value}\"", StringComparison.Ordinal);

            if (start < 0)
                return null;

            var end = text.IndexOf("local type", start + 1, StringComparison.Ordinal);
            var block = end < 0 ? text[start..] : text[start..end];
            var own = new Regex($@"\[{Regex.Escape(chrNo)}\]\s*=\s*(.*?);").Match(block);

            return own.Success ? own.Groups[1].Value : Shared().Match(block) is { Success: true } shared ? shared.Groups[1].Value : null;
        }

        [GeneratedRegex(@"-?\d+")]
        private static partial Regex NumberPattern();

        [GeneratedRegex(@"array\(\s*CHRMAX\s*,\s*(.*?)\s*\)\s*;", RegexOptions.Singleline)]
        private static partial Regex Shared();
    }
}
