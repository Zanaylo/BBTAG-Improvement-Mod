namespace BBTAG_IM.Core.Pe;

public static class OverhaulExecutable
{
    private const string ExtendSection = ".extend";

    public static bool Is(IEnumerable<PeSection> sections) => sections.Any(section => section.Name == ExtendSection);
}
