namespace BBTAG_IM.Core.Online;

public static class ByteSearch
{
    public static IReadOnlyList<int> Offsets(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        var offsets = new List<int>();

        if (needle.IsEmpty)
            return offsets;

        for (var start = 0; start <= haystack.Length - needle.Length; start++)
        {
            var found = haystack[start..].IndexOf(needle);

            if (found < 0)
                break;

            start += found;
            offsets.Add(start);
        }

        return offsets;
    }
}
