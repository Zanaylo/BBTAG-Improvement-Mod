using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class MenuText
{
    private const float WrapAtWindowEdge = 0;

    public static void Wrapped(string text)
    {
        ImGui.PushTextWrapPos(WrapAtWindowEdge);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }

    public static void Muted(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
        Wrapped(text);
        ImGui.PopStyleColor();
    }

    public static void Tooltip(string text)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            return;

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 30);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
