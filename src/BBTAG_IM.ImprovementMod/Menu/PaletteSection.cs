using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Selection;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteSection(PaletteEditor editor, PaletteChoices choices, PaletteEditorWindow editorWindow, PaletteAssignmentsWindow assignmentsWindow) : IMenuSection
{
    public const string Name = "Palettes";

    private const float ComboWidthInFontSizes = 12;
    private const int NothingWorn = -1;
    private const int GameColorsIndex = 0;
    private const int FirstPaletteIndex = 1;

    public string Title => Name;

    public void Reopened() => choices.Rescan();

    public void Draw()
    {
        if (ImGui.Button(editorWindow.IsOpen ? "Close editor" : "Open editor"))
            editorWindow.Toggle();

        ImGui.SameLine();

        if (ImGui.Button(PaletteAssignmentsWindow.Title))
            assignmentsWindow.Toggle();

        MenuText.Tooltip("Every palette of every character, and which in-game color loads each one.");

        MenuText.Wrapped("Change any color of the characters while you watch them, then save it as a palette.");

        ImGui.SeparatorText("Character palettes");

        if (!editor.InMatch)
        {
            MenuText.Muted("Palettes can be picked during a match.");
            return;
        }

        foreach (var slot in PlayerSlots.InPlayerOrder)
        {
            if (editor.Character(slot) is { } character)
                DrawChooser(character);
        }
    }

    private void DrawChooser(EditedCharacter character)
    {
        ImGui.PushID(character.Slot);
        ImGui.TextUnformatted($"P{character.Player}");
        ImGui.SameLine();
        ImGui.TextDisabled($"{character.Character.Name}, color {character.Color + 1}");

        if (choices.Of(character.Character.Folder) is { } palettes)
            DrawPaletteCombo(character, palettes);
        else
            MenuText.Muted(PaletteChoices.Reading);

        ImGui.PopID();
    }

    private void DrawPaletteCombo(EditedCharacter character, CharacterPalettes palettes)
    {
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * ComboWidthInFontSizes);

        if (ImGui.BeginCombo("##worn", PaletteChoices.Wearing(character)))
        {
            DrawChoices(character, palettes);
            ImGui.EndCombo();
        }

        WearByWheel(character, palettes);
        MenuText.Tooltip("The palette this character wears from now on in this color, on your screen only.");
        ImGui.SameLine();

        if (ImGui.Button("Rescan"))
            choices.Rescan(character.Character.Folder);

        MenuText.Tooltip($"Reads the {PaletteLibrary.FolderName}\\{character.Character.Folder} folder again, for palettes added while the game runs.");

        foreach (var problem in palettes.Problems)
            MenuText.Muted(problem);
    }

    private void DrawChoices(EditedCharacter character, CharacterPalettes palettes)
    {
        var worn = WornIndex(character, palettes);

        if (ImGui.Selectable(PaletteLibrary.GameColors, worn == GameColorsIndex))
            choices.Wear(character, null);

        MenuCombo.KeepSelectedInView(worn == GameColorsIndex);

        for (var index = 0; index < palettes.Palettes.Count; index++)
        {
            var palette = palettes.Palettes[index];
            var selected = worn == index + FirstPaletteIndex;

            if (ImGui.Selectable(palette.Name, selected))
                choices.Wear(character, palette);

            MenuCombo.KeepSelectedInView(selected);

            if (palette.Creator.Length > 0 || palette.Description.Length > 0)
                MenuText.Tooltip(Describe(palette));
        }
    }

    private void WearByWheel(EditedCharacter character, CharacterPalettes palettes)
    {
        var steps = MenuCombo.WheelSteps();

        if (steps == 0)
            return;

        var worn = WornIndex(character, palettes);
        var next = WheelChoice.Next(worn, palettes.Palettes.Count + FirstPaletteIndex, steps);

        if (next == worn)
            return;

        choices.Wear(character, next == GameColorsIndex ? null : palettes.Palettes[next - FirstPaletteIndex]);
    }

    private static int WornIndex(EditedCharacter character, CharacterPalettes palettes)
    {
        if (character.PaletteName is null)
            return character.IsEdited ? NothingWorn : GameColorsIndex;

        for (var index = 0; index < palettes.Palettes.Count; index++)
        {
            if (palettes.Palettes[index].Name.Equals(character.PaletteName, StringComparison.OrdinalIgnoreCase))
                return index + FirstPaletteIndex;
        }

        return NothingWorn;
    }

    private static string Describe(ImplPalette palette) =>
        palette.Creator.Length > 0 ? $"By {palette.Creator}. {palette.Description}".Trim() : palette.Description;
}
