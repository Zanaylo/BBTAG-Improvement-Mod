namespace BBTAG_IM.Core.Logging;

public static class ExitTrace
{
    private const string NoneFound = "none found";

    public static IReadOnlyList<uint> GameAddresses(ReadOnlySpan<uint> stack, uint moduleStart, uint moduleEnd, int limit)
    {
        List<uint> addresses = [];

        foreach (var value in stack)
        {
            if (addresses.Count == limit)
                break;

            if (value >= moduleStart && value < moduleEnd)
                addresses.Add(value);
        }

        return addresses;
    }

    public static string Describe(IReadOnlyList<uint> addresses) =>
        addresses.Count == 0 ? NoneFound : string.Join(' ', addresses.Select(address => $"0x{address:X8}"));
}
