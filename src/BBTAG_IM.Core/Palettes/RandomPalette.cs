namespace BBTAG_IM.Core.Palettes;

public static class RandomPalette
{
    public const string Any = "Random";
    public const string ExceptGameColors = "Random_Exclude_Default";

    public static bool IsChoice(string name) =>
        name.Equals(Any, StringComparison.OrdinalIgnoreCase) || name.Equals(ExceptGameColors, StringComparison.OrdinalIgnoreCase);

    public static ImplPalette? Pick(string choice, IReadOnlyList<ImplPalette> palettes, Func<int, int> pickBelow)
    {
        if (!IsChoice(choice))
            throw new ArgumentException($"{choice} is not a random choice.", nameof(choice));

        if (choice.Equals(Any, StringComparison.OrdinalIgnoreCase))
            return PickOrGameColors(palettes, pickBelow);

        return palettes.Count == 0 ? null : palettes[pickBelow(palettes.Count)];
    }

    private static ImplPalette? PickOrGameColors(IReadOnlyList<ImplPalette> palettes, Func<int, int> pickBelow)
    {
        var picked = pickBelow(palettes.Count + 1);
        return picked < palettes.Count ? palettes[picked] : null;
    }
}
