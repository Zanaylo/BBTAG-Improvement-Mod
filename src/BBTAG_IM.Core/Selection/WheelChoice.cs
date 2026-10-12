namespace BBTAG_IM.Core.Selection;

public static class WheelChoice
{
    private const int Back = -1;
    private const int Forward = 1;
    private const int Stay = 0;

    public static int Steps(float wheel) => wheel switch
    {
        > 0 => Back,
        < 0 => Forward,
        _ => Stay,
    };

    public static int Next(int current, int count, int steps)
    {
        if (count == 0 || steps == Stay)
            return current;

        return Math.Clamp(current + steps, 0, count - 1);
    }
}
