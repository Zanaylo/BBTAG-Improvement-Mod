using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PerformanceSection(MenuSettings settings, bool limiterRunning) : IMenuSection
{
    public string Title => "Performance";

    public void Reopened()
    {
    }

    public void Draw()
    {
        var frameLimiter = settings.Current.FrameLimiter;

        if (ImGui.Checkbox("Turn V-Sync off", ref frameLimiter))
            settings.Save(
                options => options.FrameLimiter = frameLimiter,
                frameLimiter ? "V-Sync turns off the next time the game starts." : "V-Sync comes back the next time the game starts.");

        MenuText.Tooltip("Turns off the native V-Sync and holds the game at 60 fps with its own timer. This can lower input delay.");
        MenuText.Muted("Changes the next time the game starts.");
        MenuText.Muted(limiterRunning ? "Running now: V-Sync off, 60 fps." : "Running now: native V-Sync.");
    }
}
