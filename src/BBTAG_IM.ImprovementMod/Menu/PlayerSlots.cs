namespace BBTAG_IM.ImprovementMod.Menu;

internal static class PlayerSlots
{
    public static readonly int[] InPlayerOrder = [0, 2, 1, 3];

    private static readonly string[] Names = ["Player 1", "Player 2", "Player 1 partner", "Player 2 partner"];

    public static string NameOf(int slot) => Names[slot];
}
