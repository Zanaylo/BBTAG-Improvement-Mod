namespace BBTAG_IM.Core.Meter;

public static class MoveNames
{
    private const string NormalPrefix = "NmlAtk";
    private const string CommonPrefix = "CmnAct";
    private const string AssistLabel = "ASSIST ";

    private static readonly (string Prefix, string Name)[] Common =
    [
        ("InvincibleAttackAir", "AIR REVERSAL"),
        ("InvincibleAttack", "REVERSAL"),
        ("CrushAttack", "CRUSH"),
    ];

    private static readonly string[] AssistPrefixes = ["ChangePartnerAssistAtk_", "AssistAtk"];

    public static string Of(string state)
    {
        if (state.StartsWith(NormalPrefix, StringComparison.Ordinal))
            return state[NormalPrefix.Length..];

        if (!state.StartsWith(CommonPrefix, StringComparison.Ordinal))
            return state;

        var name = state[CommonPrefix.Length..];

        foreach (var prefix in AssistPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return AssistLabel + name[prefix.Length..];
        }

        foreach (var (prefix, readable) in Common)
        {
            if (name.Equals(prefix, StringComparison.Ordinal))
                return readable;
        }

        return name;
    }
}
