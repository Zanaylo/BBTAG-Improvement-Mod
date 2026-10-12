using BBTAG_IM.Core.Selection;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class MenuCombo
{
    private const float Centered = 0.5f;

    public static int WheelSteps()
    {
        if (!ImGui.IsItemHovered())
            return 0;

        ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
        return WheelChoice.Steps(ImGui.GetIO().MouseWheel);
    }

    public static void KeepSelectedInView(bool selected)
    {
        if (!selected)
            return;

        ImGui.SetItemDefaultFocus();

        if (ImGui.IsWindowAppearing())
            ImGui.SetScrollHereY(Centered);
    }
}
