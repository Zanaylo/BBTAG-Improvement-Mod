namespace BBTAG_IM.Core.Scripting.Importing;

public static class BbcfFormat
{
    private static readonly Dictionary<string, string> Letters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S32"] = "i",
        ["BOOL"] = "i",
        ["Enum"] = "i",
        ["U32"] = "I",
        ["C16BYTE"] = "16s",
        ["C32BYTE"] = "32s",
        ["C64BYTE"] = "64s",
        ["C128BYTE"] = "128s",
    };

    public static string? For(IReadOnlyList<string> argumentTypes, int size)
    {
        if (!argumentTypes.All(Letters.ContainsKey))
            return null;

        var format = string.Concat(argumentTypes.Select(type => Letters[type]));
        return CommandFormat.SizeOf(CommandFormat.Parse(format)) == size ? format : null;
    }
}
