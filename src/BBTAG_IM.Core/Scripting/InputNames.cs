using System.Globalization;

namespace BBTAG_IM.Core.Scripting;

public sealed class InputNames(NameTable motions, INormalInputs normal)
{
    public const string Prefix = "INPUT_";

    private const int NormalInputCommand = 14001;
    private const string HexPrefix = "0x";

    public static readonly InputNames Empty = new(NameTable.Empty, ButtonByteInputs.Empty);

    public NameTable Motions => motions;

    public INormalInputs Normal => normal;

    public IEnumerable<NamedValue> Named => motions.Named.Concat(normal.Named).DistinctBy(value => value.Name, StringComparer.OrdinalIgnoreCase);

    public string? NameOf(int command, int value, bool hex)
    {
        var name = (command == NormalInputCommand ? normal.NameOf(value) : motions.NameOf(value)) ?? Fallback(value, hex);
        return ValueOf(name) == value ? name : null;
    }

    public int? ValueOf(string name)
    {
        if (motions.ValueOf(name) is { } motion)
            return motion;

        if (normal.ValueOf(name) is { } normalValue)
            return normalValue;

        if (!name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = name[Prefix.Length..];

        if (rest.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
            return int.TryParse(rest.AsSpan(HexPrefix.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue) ? hexValue : null;

        return int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    public InputNames With(NameTable moreMotions, INormalInputs otherNormal) => new(motions.With(moreMotions), otherNormal);

    public static string Fallback(int value, bool hex) =>
        Prefix + (hex ? HexPrefix + value.ToString("x", CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture));
}
