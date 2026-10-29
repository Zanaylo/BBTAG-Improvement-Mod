namespace BBTAG_IM.Core.Mods;

public sealed record RequiredFile(IReadOnlyList<string> Choices)
{
    public string Name => Choices[0];
}
