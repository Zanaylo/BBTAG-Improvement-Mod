using System.Globalization;

namespace BBTAG_IM.Core.Scripting;

public sealed class InputNames(NameTable motions, NameTable grouped, NameTable buttons, NameTable directions)
{
    public const string Prefix = "INPUT_";

    private const int NormalInputCommand = 14001;
    private const int ButtonShift = 8;
    private const int ByteMask = 0xFF;
    private const int HighWordMask = ~0xFFFF;
    private const string HexPrefix = "0x";

    public static readonly InputNames Empty = new(NameTable.Empty, NameTable.Empty, NameTable.Empty, NameTable.Empty);

    public string? NameOf(int command, int value, bool hex)
    {
        var name = (command == NormalInputCommand ? NormalName(value) : motions.NameOf(value)) ?? Fallback(value, hex);
        return ValueOf(name) == value ? name : null;
    }

    public int? ValueOf(string name)
    {
        if (motions.ValueOf(name) is { } motion)
            return motion;

        if (grouped.ValueOf(name) is { } group)
            return group;

        if (!name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = name[Prefix.Length..];

        if (rest.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
            return int.TryParse(rest.AsSpan(HexPrefix.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue) ? hexValue : null;

        if (int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return number;

        return ButtonAndDirection(name);
    }

    private string? NormalName(int value)
    {
        if (grouped.NameOf(value) is { } group)
            return group;

        if ((value & HighWordMask) != 0)
            return null;

        var button = buttons.NameOf((value >> ButtonShift) & ByteMask);
        var direction = directions.NameOf(value & ByteMask);

        return button is null || direction is null ? null : direction + button;
    }

    private int? ButtonAndDirection(string name)
    {
        var button = buttons.ValueOf(name[^1..]);
        var direction = directions.ValueOf(name[..^1]);

        if (button is null || direction is null)
            return null;

        return (button.Value << ButtonShift) + direction.Value;
    }

    private static string Fallback(int value, bool hex) =>
        Prefix + (hex ? HexPrefix + value.ToString("x", CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture));
}
