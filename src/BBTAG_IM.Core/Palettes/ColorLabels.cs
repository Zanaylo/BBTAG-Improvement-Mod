namespace BBTAG_IM.Core.Palettes;

public static class ColorLabels
{
    public const string NotAssigned = "Not assigned";
    public const string RandomLabel = "Random";
    public const string RandomExceptGameColorsLabel = "Random (exclude game colors)";

    private const int LastColor = 0xFF;

    private static readonly string[] Labels = Enumerable.Range(0, LastColor + 1).Select(color => $"Color {color}").ToArray();

    public static string Of(int color) => color is >= 0 and <= LastColor ? Labels[color] : $"Color {color}";

    public static string Shown(string assignment)
    {
        if (assignment.Equals(RandomPalette.Any, StringComparison.OrdinalIgnoreCase))
            return RandomLabel;

        return assignment.Equals(RandomPalette.ExceptGameColors, StringComparison.OrdinalIgnoreCase) ? RandomExceptGameColorsLabel : assignment;
    }
}
