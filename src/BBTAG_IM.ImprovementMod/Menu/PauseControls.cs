using BBTAG_IM.ImprovementMod.Training;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PauseControls(MatchPause? pause)
{
    public void Draw()
    {
        if (pause is null)
        {
            MenuText.Muted("Pause is not available in this game version.");
            return;
        }

        var paused = pause.IsPaused;
        ImGui.BeginDisabled(!pause.CanPause);

        if (ImGui.Checkbox("Pause", ref paused))
            pause.Toggle();

        ImGui.SameLine();
        ImGui.TextDisabled($"({Hotkeys.NameOf(Hotkey.Pause)})");
        ImGui.SameLine();
        ImGui.BeginDisabled(!paused);

        if (ImGui.Button("Next frame"))
            pause.Step();

        if (ImGui.IsItemActive())
            pause.HoldingNextFrameButton();

        MenuText.Tooltip("Plays one frame. Hold it to keep playing frames slowly.");
        ImGui.SameLine();
        ImGui.TextDisabled($"({Hotkeys.NameOf(Hotkey.NextFrame)})");
        ImGui.EndDisabled();
        ImGui.EndDisabled();

        if (!pause.CanPause)
            MenuText.Muted("Pause works during training matches.");
    }
}
