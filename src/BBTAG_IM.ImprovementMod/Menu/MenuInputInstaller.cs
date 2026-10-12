using BBTAG_IM.Core.Logging;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Graphics;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class MenuInputInstaller(ILog log)
{
    private bool _tried;

    public void OnPresent(nint device)
    {
        if (_tried)
            return;

        _tried = true;

        if (MenuInput.Install(new Direct3DDevice(device).FocusWindow()))
            SafeLog.Info(log, $"Press {MenuInput.ToggleKeyName} in the game to open the Improvement Mod menu. In training, {Hotkeys.NameOf(Hotkey.Hitboxes)} shows the hitboxes, {Hotkeys.NameOf(Hotkey.Pause)} pauses and {Hotkeys.NameOf(Hotkey.NextFrame)} plays the next frame.");
        else
            SafeLog.Error(log, $"The game window could not be found, so the {MenuInput.ToggleKeyName}, {Hotkeys.NameOf(Hotkey.Hitboxes)}, {Hotkeys.NameOf(Hotkey.Pause)} and {Hotkeys.NameOf(Hotkey.NextFrame)} keys do nothing.");
    }
}
