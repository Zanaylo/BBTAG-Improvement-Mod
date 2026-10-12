using System.Numerics;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class PromptButtons
{
    private const float TextWidthInFontSizes = 24;
    private const float ButtonWidthInFontSizes = 7;
    private const int DangerColors = 3;

    private static readonly Vector4 Danger = new(0.72f, 0.18f, 0.18f, 1);
    private static readonly Vector4 DangerHovered = new(0.85f, 0.25f, 0.25f, 1);
    private static readonly Vector4 DangerActive = new(0.60f, 0.12f, 0.12f, 1);

    public static float TextWidth => ImGui.GetFontSize() * TextWidthInFontSizes;

    public static bool Button(string label) => ImGui.Button(label, new Vector2(ImGui.GetFontSize() * ButtonWidthInFontSizes, 0));

    public static bool DangerButton(string label)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, Danger);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, DangerHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, DangerActive);
        var pressed = Button(label);
        ImGui.PopStyleColor(DangerColors);
        return pressed;
    }
}
