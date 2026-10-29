namespace BBTAG_IM.ImprovementMod.Files;

internal sealed record FileSubstitution(
    string Marker,
    Func<string, string?> Substitute,
    Func<string, string> Served,
    Func<string, string, string> Failed);
