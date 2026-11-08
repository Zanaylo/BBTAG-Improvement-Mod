using System.Numerics;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class MenuText
{
    private const float WrapAtWindowEdge = 0;
    private const string HelpMark = "(?)";
    private const float TooltipWidthInFontSizes = 30;

    public static void Wrapped(string text)
    {
        ImGui.PushTextWrapPos(WrapAtWindowEdge);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }

    public static void Muted(string text) => Colored(ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled], text);

    public static void Colored(Vector4 color, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        Wrapped(text);
        ImGui.PopStyleColor();
    }

    public static void HelpMarker(string text)
    {
        ImGui.TextDisabled(HelpMark);
        Tooltip(text);
    }

    public static void Tooltip(string text)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            return;

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * TooltipWidthInFontSizes);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
