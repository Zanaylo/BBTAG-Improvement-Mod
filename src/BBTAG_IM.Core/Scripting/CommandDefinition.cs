namespace BBTAG_IM.Core.Scripting;

public sealed record CommandDefinition(
    int Id,
    string Name,
    IReadOnlyList<CommandArgument> Arguments,
    IReadOnlySet<int> SlotTypeArguments,
    HexArguments Hex)
{
    private const string UnknownPrefix = "Unknown";

    public int Size => CommandFormat.SizeOf(Arguments);

    public bool HasSlotArguments => SlotTypeArguments.Count > 0;

    public static string UnknownName(int id) => UnknownPrefix + id;

    public static int? IdFromUnknownName(string name) =>
        name.StartsWith(UnknownPrefix, StringComparison.OrdinalIgnoreCase)
        && int.TryParse(name.AsSpan(UnknownPrefix.Length), out var id)
            ? id
            : null;
}
