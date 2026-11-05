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
        DrawAdvancedMapperButtons();
    }

    private void DrawAdvancedMapperButtons()
    {
        var advanced = settings.Current.AdvancedMapperButtons;

        if (ImGui.Checkbox("Advanced mapper buttons", ref advanced))
            settings.Save(
                options => options.AdvancedMapperButtons = advanced,
                advanced ? "E shows in the button settings the next time the game starts." : "The button settings go back to the game's rows the next time the game starts.");

        MenuText.Tooltip("Adds the E button to the native button settings, for the controller and the keyboard, so it can be set like A, B, C and D.");
        MenuText.Muted("Changes the next time the game starts.");
    }

    private void SaveFontSize(float? size)
    {
        _dragged = null;
        settings.Save(options => options.MenuFontSize = size);
    }
}
