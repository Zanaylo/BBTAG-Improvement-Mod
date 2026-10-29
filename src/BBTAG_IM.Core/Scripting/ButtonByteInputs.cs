namespace BBTAG_IM.Core.Scripting;

public sealed class ButtonByteInputs(NameTable grouped, NameTable buttons, NameTable directions) : INormalInputs
{
    private const int ButtonShift = 8;
    private const int ByteMask = 0xFF;
    private const int HighWordMask = ~0xFFFF;

    public static readonly ButtonByteInputs Empty = new(NameTable.Empty, NameTable.Empty, NameTable.Empty);

    public IEnumerable<NamedValue> Named =>
        grouped.Named.Concat(
            from button in buttons.Named
            from direction in directions.Named
            select new NamedValue((button.Value << ButtonShift) + direction.Value, direction.Name + button.Name));

    public string? NameOf(int value)
    {
        if (grouped.NameOf(value) is { } group)
            return group;

        if ((value & HighWordMask) != 0)
            return null;

        var button = buttons.NameOf((value >> ButtonShift) & ByteMask);
        var direction = directions.NameOf(value & ByteMask);

        return button is null || direction is null ? null : direction + button;
    }

    public int? ValueOf(string name)
    {
        if (grouped.ValueOf(name) is { } group)
            return group;

        if (name.Length < 2)
            return null;

        var button = buttons.ValueOf(name[^1..]);
        var direction = directions.ValueOf(name[..^1]);

        if (button is null || direction is null)
            return null;

        return (button.Value << ButtonShift) + direction.Value;
    }
}
