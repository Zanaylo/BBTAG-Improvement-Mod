using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Hitboxes;

public static class FateScale
{
    private const float Native = 1f;
    private const float UnderNight = 1.05f;
    private const float Persona = 0.95f;

    public static float Of(Fate? fate) => fate switch
    {
        Fate.UnderNight => UnderNight,
        Fate.Persona => Persona,
        _ => Native,
    };
}
