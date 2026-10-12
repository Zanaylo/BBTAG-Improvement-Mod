using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class MenuKeys
{
    private const char FirstPrintable = ' ';

    private static readonly Dictionary<int, ImGuiKey> Keys = new()
    {
        [0x08] = ImGuiKey.Backspace,
        [0x09] = ImGuiKey.Tab,
        [0x0D] = ImGuiKey.Enter,
        [0x10] = ImGuiKey.ModShift,
        [0x11] = ImGuiKey.ModCtrl,
        [0x1B] = ImGuiKey.Escape,
        [0x23] = ImGuiKey.End,
        [0x24] = ImGuiKey.Home,
        [0x25] = ImGuiKey.LeftArrow,
        [0x26] = ImGuiKey.UpArrow,
        [0x27] = ImGuiKey.RightArrow,
        [0x28] = ImGuiKey.DownArrow,
        [0x2E] = ImGuiKey.Delete,
        [0x41] = ImGuiKey.A,
        [0x43] = ImGuiKey.C,
        [0x56] = ImGuiKey.V,
        [0x58] = ImGuiKey.X,
        [0x5A] = ImGuiKey.Z,
    };

    public static void Press(ImGuiIOPtr io, int virtualKey, bool down)
    {
        if (Keys.TryGetValue(virtualKey, out var key))
            io.AddKeyEvent(key, down);
    }

    public static void Type(ImGuiIOPtr io, char character)
    {
        if (character >= FirstPrintable)
            io.AddInputCharacter(character);
    }
}
