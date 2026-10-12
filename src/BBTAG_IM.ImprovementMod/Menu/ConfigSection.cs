using BBTAG_IM.Core.Overlay;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class ConfigSection(MenuSettings settings) : IMenuSection
{
    private const float SliderWidthInFontSizes = 10;

    private float? _dragged;

    public string Title => "Config";

    public void Reopened() => _dragged = null;

    public void Draw()
    {
        var size = _dragged ?? MenuFont.SizeFor(settings.Current.MenuFontSize, ImGui.GetIO().DisplaySize.Y);
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * SliderWidthInFontSizes);

        if (ImGui.SliderFloat("Font size", ref size, MenuFont.Smallest, MenuFont.Largest, "%.0f px", ImGuiSliderFlags.AlwaysClamp))
            _dragged = size;

        if (ImGui.IsItemDeactivatedAfterEdit())
            SaveFontSize(MathF.Round(size));

        MenuText.Tooltip("The size of the text in this menu. It changes when you let go of the slider.");
        ImGui.SameLine();
        ImGui.BeginDisabled(settings.Current.MenuFontSize is null);

        if (ImGui.Button("Default"))
            SaveFontSize(null);

        ImGui.EndDisabled();
        MenuText.Tooltip("Goes back to a size that follows the screen height.");
    }

    private void SaveFontSize(float? size)
    {
        _dragged = null;
        settings.Save(options => options.MenuFontSize = size);
    }
}
