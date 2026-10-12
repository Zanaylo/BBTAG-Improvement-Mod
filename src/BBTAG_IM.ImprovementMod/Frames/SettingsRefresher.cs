using System.Diagnostics;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.ImprovementMod.Frames;

internal sealed class SettingsRefresher(SettingsWatcher<ImprovementSettings> settings, ILog log)
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private long _lastCheck = Stopwatch.GetTimestamp();

    public void OnPresent(nint device)
    {
        if (Stopwatch.GetElapsedTime(_lastCheck) < Interval)
            return;

        _lastCheck = Stopwatch.GetTimestamp();

        if (settings.Refresh())
            SafeLog.Info(log, $"Options reloaded. Hitbox viewer {(settings.Current.HitboxViewer ? "on" : "off")}, palettes chosen for {settings.Current.Palettes.Count} characters.");
    }
}
