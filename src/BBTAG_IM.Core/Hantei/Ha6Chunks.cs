namespace BBTAG_IM.Core.Hantei;

internal static class Ha6Chunks
{
    private const int PrefixLength = 3;
    private const string LiteralDuration = "AFD";
    private const string DeprecatedOffset = "AFY";
    private const string AttackSuffix = "ATS";
    private const char DeprecatedOffsetFinal = 'X';

    private static readonly Dictionary<string, int> FixedLengths = new()
    {
        ["HRNM"] = 5, ["HRAT"] = 5, ["HRNS"] = 2, ["HRAS"] = 2,
        ["ASSM"] = 1, ["FSNA"] = 1, ["FSNH"] = 1, ["FSNE"] = 1, ["FSNI"] = 1,
        ["ATGD"] = 1, ["ATV2"] = 14, ["ATVD"] = 1, ["ATHS"] = 1, ["ATVV"] = 2, ["ATHT"] = 1, ["ATHH"] = 1,
        ["ATF1"] = 1, ["ATAT"] = 1, ["ATAM"] = 1, ["ATCA"] = 1, ["ATC0"] = 3, ["ATHE"] = 2,
        ["ATKK"] = 1, ["ATNG"] = 1, ["ATUH"] = 1, ["ATBT"] = 1, ["ATSN"] = 1, ["ATSU"] = 1,
        ["ATSP"] = 1, ["ATSA"] = 1, ["ATSH"] = 1, ["ATGN"] = 1, ["ATBC"] = 1, ["ATRF"] = 1,
        ["ASV0"] = 5, ["ASVX"] = 0, ["ASMV"] = 1, ["ASS1"] = 0, ["ASS2"] = 0,
        ["ASCN"] = 1, ["ASCS"] = 1, ["ASCT"] = 1, ["ASCF"] = 1, ["AST0"] = 7,
        ["ASMX"] = 1, ["ASAA"] = 1, ["ASYS"] = 1, ["ASF0"] = 1, ["ASF1"] = 1,
        ["EFTP"] = 1, ["EFNO"] = 1, ["IFTP"] = 1,
        ["AFGP"] = 2, ["AFGX"] = 3, ["AFOF"] = 2, ["AFPL"] = 1, ["AFAL"] = 2, ["AFRG"] = 3,
        ["AFAZ"] = 1, ["AFAY"] = 1, ["AFAX"] = 1, ["AFZM"] = 2, ["AFJH"] = 1, ["AFJP"] = 1,
        ["AFHK"] = 1, ["AFPA"] = 1, ["AFPR"] = 1, ["AFCT"] = 1, ["AFLP"] = 1, ["AFJC"] = 1,
        ["AFTN"] = 2, ["AFRT"] = 1, ["AFID"] = 1, ["AFDL"] = 1, ["AFFL"] = 1, ["AFFE"] = 1,
        ["AFF1"] = 0, ["AFF2"] = 0,
        ["PSTS"] = 1, ["PLVL"] = 1, ["PFLG"] = 1, ["PUPS"] = 1, ["PDST"] = 1, ["PTIT"] = 8,
    };

    private static readonly HashSet<string> CountedChunks = ["ATGV", "ATHV", "EFPR", "IFPR"];

    public static bool IsCounted(string tag) => CountedChunks.Contains(tag);

    public static int? FixedLength(string tag)
    {
        if (FixedLengths.TryGetValue(tag, out var length))
            return length;

        return IsSuffixValue(tag) ? 0 : null;
    }

    public static bool IsLiteralDuration(string tag) => tag.StartsWith(LiteralDuration, StringComparison.Ordinal) && char.IsAsciiDigit(tag[^1]);

    private static bool IsSuffixValue(string tag)
    {
        var prefix = tag[..PrefixLength];
        var last = tag[^1];

        return prefix switch
        {
            LiteralDuration or AttackSuffix => char.IsAsciiDigit(last),
            DeprecatedOffset => char.IsAsciiDigit(last) || last == DeprecatedOffsetFinal,
            _ => false,
        };
    }
}
