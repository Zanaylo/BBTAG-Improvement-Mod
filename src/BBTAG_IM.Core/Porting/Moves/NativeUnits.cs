namespace BBTAG_IM.Core.Porting.Moves;

public static class NativeUnits
{
    private const double GameUnitsPerPixel = 1000.0;
    private const double HanteiUnitsPerPixel = 128.0;

    public const int Position = 1000;

    public static int Speed(int hanteiSpeed) => (int)Math.Round(hanteiSpeed * GameUnitsPerPixel / HanteiUnitsPerPixel);
}
