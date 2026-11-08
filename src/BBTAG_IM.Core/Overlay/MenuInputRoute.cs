namespace BBTAG_IM.Core.Overlay;

public readonly record struct MenuInputRoute(bool ReachesMenu, bool ReachesGame)
{
    private const int ShiftKey = 0x10;
    private const int ControlKey = 0x11;

    public static IReadOnlyList<int> ModifierKeys { get; } = [ShiftKey, ControlKey];

    public static MenuInputRoute Of(WindowInput input, bool typing) => input switch
    {
        KeyPressed { Key: ShiftKey or ControlKey } when !typing => new(ReachesMenu: true, ReachesGame: true),
        KeyPressed or CharacterTyped when !typing => new(ReachesMenu: false, ReachesGame: true),
        KeyReleased => new(ReachesMenu: true, ReachesGame: !typing),
        FocusLost => new(ReachesMenu: true, ReachesGame: true),
        _ => new(ReachesMenu: true, ReachesGame: false),
    };
}
