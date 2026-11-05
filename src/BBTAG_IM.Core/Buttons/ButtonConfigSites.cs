namespace BBTAG_IM.Core.Buttons;

public sealed record MenuButtonSites(uint Vtable, uint ApplyTail);

public sealed record ControllerRowDrawing(uint Site, uint NotASlot, int Buffer, uint StringConstructor, uint CommonDraw);

public sealed record ButtonConfigSites(
    MenuButtonSites Controller,
    MenuButtonSites Keyboard,
    uint ItemConstructor,
    uint MenusGuard,
    uint KeyboardDrawBound,
    ControllerRowDrawing ControllerDraw,
    uint IconResolver,
    uint ConfigRoot,
    uint BattleButtons)
{
    public MenuButtonSites For(ButtonConfigDevice device) => device == ButtonConfigDevice.Controller ? Controller : Keyboard;
}
