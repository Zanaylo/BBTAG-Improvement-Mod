using System.Numerics;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class FittedWindow
{
    private const float NarrowestInFontSizes = 26;
    private const float FitContent = 0;

    public static bool Begin(string title, ref bool isOpen, Vector2 firstPosition)
    {
        ImGui.SetNextWindowPos(firstPosition, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(FitContent), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(ImGui.GetFontSize() * NarrowestInFontSizes, FitContent), new Vector2(float.MaxValue));
        return ImGui.Begin(title, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize);
    }
}
