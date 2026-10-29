using System.Globalization;

namespace BBTAG_IM.Core.Harness;

public static class HarnessKeys
{
    private const string HexPrefix = "0x";
    private const string NumberPadPrefix = "NUMPAD";
    private const string FunctionPrefix = "F";
    private const int NumberPadZero = 0x60;
    private const int FunctionOne = 0x70;
    private const int FunctionKeys = 12;
    private const int HighestKey = 0xFF;

    private static readonly Dictionary<string, int> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ENTER"] = 0x0D,
        ["ESCAPE"] = 0x1B,
        ["SPACE"] = 0x20,
        ["LEFT"] = 0x25,
        ["UP"] = 0x26,
        ["RIGHT"] = 0x27,
        ["DOWN"] = 0x28,
        ["SHIFT"] = 0x10,
        ["CONTROL"] = 0x11,
        ["TAB"] = 0x09,
        ["BACK"] = 0x08,
    };

    public static int? Key(string name)
    {
        if (name.Length == 0)
            return null;

        if (Named.TryGetValue(name, out var named))
            return named;

        if (name.Length == 1 && char.IsAsciiLetterOrDigit(name[0]))
            return char.ToUpperInvariant(name[0]);

        if (name.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
            return Hex(name[HexPrefix.Length..]);

        if (name.StartsWith(NumberPadPrefix, StringComparison.OrdinalIgnoreCase))
            return Numbered(name[NumberPadPrefix.Length..], NumberPadZero, 0, 9);

        return name.StartsWith(FunctionPrefix, StringComparison.OrdinalIgnoreCase)
            ? Numbered(name[FunctionPrefix.Length..], FunctionOne - 1, 1, FunctionKeys)
            : null;
    }

    private static int? Hex(string digits) =>
        int.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) && value is > 0 and <= HighestKey ? value : null;

    private static int? Numbered(string digits, int first, int lowest, int highest) =>
        int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= lowest && number <= highest
            ? first + number
            : null;
}
