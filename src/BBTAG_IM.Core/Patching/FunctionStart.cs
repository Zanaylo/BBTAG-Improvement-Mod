namespace BBTAG_IM.Core.Patching;

public static class FunctionStart
{
    public const int FallbackLead = 64;

    private const byte Int3Padding = 0xCC;
    private const uint FunctionAlignment = 16;
    private const int MaximumSearch = 0x10000;

    public static int AnchorBefore(ReadOnlySpan<byte> code, uint codeRva, int offset)
    {
        var limit = Math.Max(1, offset - MaximumSearch);

        for (var candidate = Math.Min(offset, code.Length - 1); candidate >= limit; candidate--)
        {
            if (IsFunctionStart(code, codeRva, candidate))
                return candidate;
        }

        return Math.Max(0, offset - FallbackLead);
    }

    private static bool IsFunctionStart(ReadOnlySpan<byte> code, uint codeRva, int candidate) =>
        (codeRva + (uint)candidate) % FunctionAlignment == 0
        && code[candidate - 1] == Int3Padding
        && code[candidate] != Int3Padding;
}
