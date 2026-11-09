namespace BBTAG_IM.ImprovementMod.Menu;

internal enum Hotkey
{
    Hitboxes,
    FrameMeter,
    Pause,
    NextFrame,
}

internal static class Hotkeys
{
    private static readonly HotkeyBinding[] s_bindings =
    [
        new(Hotkey.Hitboxes, 0x71, "F2"),
        new(Hotkey.FrameMeter, 0x72, "F3"),
        new(Hotkey.Pause, 0x74, "F5"),
        new(Hotkey.NextFrame, 0x75, "F6"),
    ];

    public static int Count => s_bindings.Length;

    public static string NameOf(Hotkey hotkey) => s_bindings[(int)hotkey].Name;

    public static int KeyOf(Hotkey hotkey) => s_bindings[(int)hotkey].Key;

    public static Hotkey? Of(int key) => Array.Find(s_bindings, binding => binding.Key == key)?.Hotkey;

    private sealed record HotkeyBinding(Hotkey Hotkey, int Key, string Name);
}
