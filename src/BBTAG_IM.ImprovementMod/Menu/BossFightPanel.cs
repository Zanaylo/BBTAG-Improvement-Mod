using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Selection;
using BBTAG_IM.ImprovementMod.Training;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class BossFightPanel(BossFightControl bossFight) : ITrainingPanel
{
    private const string NoBossLabel = "Off";

    public void Draw()
    {
        ImGui.SeparatorText("Boss fight");
        Boss?[] choices = [null, .. bossFight.Bosses];
        var current = Array.IndexOf(choices, bossFight.Chosen);

        if (ImGui.BeginCombo("Boss", NameOf(choices[current])))
        {
            for (var choice = 0; choice < choices.Length; choice++)
                DrawBossChoice(choices[choice], choice == current);

            ImGui.EndCombo();
        }

        var next = WheelChoice.Next(current, choices.Length, MenuCombo.WheelSteps());

        if (next != current)
            bossFight.Choose(choices[next]);

        MenuText.Tooltip("Needs the overhaul with its table turned on. Pick a boss, then confirm any team for Player 2 and any stage on character select. Player 2 becomes the boss alone and the match is played on the boss stage. This is a test and can still crash. It turns off when the game closes.");

        if (bossFight.NotReady is { } notReady)
            MenuText.Muted(notReady);
    }

    private void DrawBossChoice(Boss? boss, bool selected)
    {
        if (ImGui.Selectable(NameOf(boss), selected) && !selected)
            bossFight.Choose(boss);

        MenuCombo.KeepSelectedInView(selected);
    }

    private static string NameOf(Boss? boss) => boss?.Name ?? NoBossLabel;
}
