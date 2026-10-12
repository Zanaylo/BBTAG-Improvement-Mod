using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.ImprovementMod.Graphics;
using BBTAG_IM.ImprovementMod.Menu;

namespace BBTAG_IM.ImprovementMod.Frames;

internal sealed class HitboxViewer(GameView game, SettingsWatcher<ImprovementSettings> settings, HitboxRenderer renderer, ILog log)
{
    public void OnPresent(nint device)
    {
        if (MenuInput.TakePresses(Hotkey.Hitboxes) % 2 == 1)
            Toggle();

        if (!HitboxScene.IsVisible(game, settings.Current) || game.Camera() is not { } camera)
            return;

        var frame = HitboxScene.Collect(game);

        if (frame.Boxes.Count > 0)
            renderer.Draw(device, frame, camera, settings.Current);
    }

    private void Toggle()
    {
        try
        {
            settings.Change(options => options.HitboxViewer = !options.HitboxViewer);
            SafeLog.Info(log, settings.Current.HitboxViewer ? "Hitbox viewer on." : "Hitbox viewer off.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SafeLog.Error(log, $"The hitbox viewer choice could not be saved: {exception.Message}");
        }
    }
}
