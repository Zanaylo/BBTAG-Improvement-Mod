namespace BBTAG_IM.Core.Buttons;

public enum ButtonConfigDevice
{
    Controller,
    Keyboard,
}

public sealed record ExtraButton(string Name, byte Logical, ushort BattleBit, string RowKey, string Label, string IconTag)
{
    public uint PressedMask => 1u << Logical;
}

public static class ExtraButtons
{
    public const byte Unassigned = 0xFF;
    public const string StandInIconTag = "AttackAB";
    public const int StandInSlot = 5;

    private const int RowsBeforeTheFirstSlot = 2;
    private const int ControllerGameRows = 14;
    private const int KeyboardGameRows = 18;
    private const int ControllerTableIndexBase = 0x34C;
    private const int ControllerTableLength = 0x14;

    public static ExtraButton E { get; } = new("E", 0x10, 0x100, "KYCF_AttackE", "^mCMDS;Button E", "AttackE");

    public static IReadOnlyList<ExtraButton> All { get; } = [E];

    public static IReadOnlyList<KeyValuePair<string, string>> TextEntries { get; } =
        [.. All.Select(button => new KeyValuePair<string, string>(button.RowKey, button.Label))];

    public static ExtraButton? FromIconTag(string tag) => All.FirstOrDefault(button => button.IconTag == tag);

    public static int ControllerTableOffset(int player) => (player + ControllerTableIndexBase) * ControllerTableLength;

    public static uint WithE(uint pressed, uint battleButtons) =>
        (pressed & E.PressedMask) != 0 ? battleButtons | E.BattleBit : battleButtons;

    public static int GameRows(ButtonConfigDevice device) => device == ButtonConfigDevice.Controller ? ControllerGameRows : KeyboardGameRows;

    public static int Rows(ButtonConfigDevice device) => GameRows(device) + All.Count;

    public static ExtraButton? AtRow(ButtonConfigDevice device, int row)
    {
        var position = row - GameRows(device);
        return position >= 0 && position < All.Count ? All[position] : null;
    }

    public static int SlotOf(ButtonConfigDevice device, ExtraButton button)
    {
        var position = All.ToList().IndexOf(button);
        return GameRows(device) + position - RowsBeforeTheFirstSlot;
    }
}
