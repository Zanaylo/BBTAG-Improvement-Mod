namespace BBTAG_IM.Core.Overlay;

public static class MenuFont
{
    public const float Smallest = 10;
    public const float Largest = 32;

    private const float SmallestAutomatic = 16;
    private const float SizePerScreenHeight = 1f / 50;

    public static float SizeFor(float? chosen, float screenHeight)
    {
        var size = chosen ?? MathF.Max(SmallestAutomatic, screenHeight * SizePerScreenHeight);
        return Math.Clamp(MathF.Round(size), Smallest, Largest);
    }
}
