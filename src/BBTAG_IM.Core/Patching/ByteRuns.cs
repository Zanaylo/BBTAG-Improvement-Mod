namespace BBTAG_IM.Core.Patching;

internal readonly record struct ByteRun(int Start, int End)
{
    public int Length => End - Start;
}

internal static class ByteRuns
{
    public const int AddressSize = sizeof(uint);

    public static List<ByteRun> Changed(ReadOnlySpan<byte> stock, ReadOnlySpan<byte> modded)
    {
        var runs = new List<ByteRun>();

        for (var offset = 0; offset < stock.Length;)
        {
            var start = offset + stock[offset..].CommonPrefixLength(modded[offset..]);

            if (start >= stock.Length)
                break;

            var end = start;

            while (end < stock.Length && stock[end] != modded[end])
                end++;

            runs.Add(new ByteRun(start, end));
            offset = end;
        }

        return runs;
    }

    public static ByteRun WidenOverFields(ByteRun run, Func<int, bool> isFieldStart, int length)
    {
        var (start, end) = run;

        for (var widened = true; widened;)
        {
            widened = false;

            for (var offset = Math.Max(0, start - AddressSize + 1); offset < end; offset++)
            {
                if (!isFieldStart(offset))
                    continue;

                var fieldEnd = Math.Min(length, offset + AddressSize);
                widened |= offset < start || fieldEnd > end;
                start = Math.Min(start, offset);
                end = Math.Max(end, fieldEnd);
            }
        }

        return new ByteRun(start, end);
    }

    public static List<ByteRun> Merge(IEnumerable<ByteRun> runs)
    {
        var merged = new List<ByteRun>();

        foreach (var run in runs.OrderBy(run => run.Start))
        {
            if (merged.Count > 0 && run.Start <= merged[^1].End)
            {
                merged[^1] = merged[^1] with { End = Math.Max(merged[^1].End, run.End) };
                continue;
            }

            merged.Add(run);
        }

        return merged;
    }
}
