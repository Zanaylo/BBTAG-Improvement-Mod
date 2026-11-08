using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Hantei;

public sealed record HitVector(int Number, bool Launches, int Level, int SpeedX, int SpeedY, int? Stun = null);

public sealed partial class HitVectorTable
{
    private const string EntryStart = "vectmpl.add(";
    private const string LaunchType = "uki";
    private const string GuardType = "guard";
    private const int DefaultLevel = 3;
    private const int FirstLevel = 2;
    private const int HighestLevel = 5;

    private readonly Dictionary<int, HitVector> _vectors;

    private HitVectorTable(Dictionary<int, HitVector> vectors) => _vectors = vectors;

    public static HitVectorTable Parse(string text)
    {
        Dictionary<int, (int X, int Y)> speeds = [];

        foreach (Match row in SpeedRow().Matches(text))
            speeds[int.Parse(row.Groups[1].Value)] = (int.Parse(row.Groups[2].Value), int.Parse(row.Groups[3].Value));

        Dictionary<string, int> stuns = [];

        foreach (Match time in StunTime().Matches(text))
            stuns[time.Groups[1].Value] = int.Parse(time.Groups[2].Value);

        Dictionary<int, HitVector> vectors = [];

        foreach (var entry in text.Split(EntryStart).Skip(1))
        {
            if (Number().Match(entry) is not { Success: true } number)
                continue;

            var id = int.Parse(number.Groups[1].Value);
            var speed = VectorNumber().Match(entry) is { Success: true } vector ? speeds.GetValueOrDefault(int.Parse(vector.Groups[1].Value)) : default;
            vectors[id] = new HitVector(id, IsLaunch(entry), LevelOf(entry), speed.X, speed.Y, StunOf(entry, stuns));
        }

        return new HitVectorTable(vectors);
    }

    public HitVector? Find(int number) => _vectors.GetValueOrDefault(number);

    [GeneratedRegex(@"\[\s*(\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*-?\d+\s*,\s*-?\d+\s*\]")]
    private static partial Regex SpeedRow();

    [GeneratedRegex(@"number\s*=\s*(\d+)")]
    private static partial Regex Number();

    [GeneratedRegex(@"vector_num\s*=\s*(\d+)")]
    private static partial Regex VectorNumber();

    [GeneratedRegex(@"type\s*=\s*""(\w+)""")]
    private static partial Regex Type();

    [GeneratedRegex(@"def_VL_AtkLv_([A-E])")]
    private static partial Regex Power();

    [GeneratedRegex(@"def_VL_AtkLv_([A-E]\d)")]
    private static partial Regex FullPower();

    [GeneratedRegex(@"const\s+def_VL_VT_(\w+)\s*=\s*(\d+)")]
    private static partial Regex StunTime();

    private static bool IsLaunch(string entry) => Type().Match(entry).Groups[1].Value.StartsWith(LaunchType, StringComparison.Ordinal);

    private static int? StunOf(string entry, Dictionary<string, int> stuns)
    {
        var power = FullPower().Match(entry);

        if (!power.Success)
            return null;

        var family = Type().Match(entry).Groups[1].Value.StartsWith(GuardType, StringComparison.Ordinal) ? "Guard_Std_" : "Hit_Std_";
        return stuns.TryGetValue(family + power.Groups[1].Value, out var stun) ? stun : null;
    }

    private static int LevelOf(string entry)
    {
        var power = Power().Match(entry);
        return power.Success ? Math.Min(FirstLevel + (power.Groups[1].Value[0] - 'A'), HighestLevel) : DefaultLevel;
    }
}
