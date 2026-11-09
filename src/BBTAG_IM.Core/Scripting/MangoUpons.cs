using System.Globalization;

namespace BBTAG_IM.Core.Scripting;

public static class MangoUpons
{
    private static readonly Dictionary<int, string> Names = new()
    {
        [0] = "IMMEDIATE",
        [1] = "STATE_END",
        [2] = "LANDING",
        [10] = "ON_HIT_OR_BLOCK",
    };

    private static readonly Dictionary<string, int> Numbers =
        Names.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public static string NameOf(int upon) => Names.GetValueOrDefault(upon) ?? upon.ToString(CultureInfo.InvariantCulture);

    public static int? ValueOf(string name) => Numbers.TryGetValue(name, out var upon) ? upon : null;
}
