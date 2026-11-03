namespace BBTAG_IM.Core.Palettes;

public static class PaletteGradient
{
    public static PaletteColor[] Between(PaletteColor from, PaletteColor to, int count)
    {
        if (count <= 0)
            return [];

        if (count == 1)
            return [from];

        var lastStep = count - 1;
        return Enumerable.Range(0, count).Select(step => Blend(from, to, (double)step / lastStep)).ToArray();
    }

    private static PaletteColor Blend(PaletteColor from, PaletteColor to, double amount) => new(
        Blend(from.R, to.R, amount),
        Blend(from.G, to.G, amount),
        Blend(from.B, to.B, amount),
        Blend(from.A, to.A, amount));

    private static byte Blend(byte from, byte to, double amount) =>
        (byte)Math.Round(from + (to - from) * amount, MidpointRounding.AwayFromZero);
}
