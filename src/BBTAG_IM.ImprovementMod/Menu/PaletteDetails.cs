using System.Numerics;
using BBTAG_IM.Core.Palettes;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteDetails(
    PaletteEditor editor,
    PaletteEditorWindow editorWindow,
    PaletteSelection selection,
    ColorAssignmentCombo combo,
    SpriteThumbnails thumbnails,
    PaletteFiles files,
    PaletteDeletion deletion)
{
    private const int ButtonRows = 3;
    private const float RightEdge = -float.Epsilon;
    private const float PreviewStep = 8;

    private static readonly Vector4 DeleteText = new(1, 0.42f, 0.42f, 1);

    public void Draw()
    {
        if (selection.Current() is not { } palette)
        {
            MenuText.Wrapped("Pick a palette on the left to see it here.");
            return;
        }

        var folder = selection.Folder;
        DrawDescription(folder, palette);

        ImGui.Spacing();
        ImGui.TextUnformatted("In-game color");
        ImGui.SetNextItemWidth(RightEdge);
        combo.Draw(folder, palette.Name);
        MenuText.Tooltip("Picking this color in game loads the palette automatically.");

        ImGui.Spacing();
        DrawPreview(folder, palette);
        DrawButtons(folder, palette);
    }

    private static void DrawDescription(string folder, ImplPalette palette)
    {
        ImGui.TextDisabled(folder);
        MenuText.Wrapped(palette.Name);

        if (palette.Creator.Length > 0)
        {
            ImGui.TextDisabled("by");
            ImGui.SameLine();
            ImGui.TextDisabled(palette.Creator);
        }

        if (palette.Description.Length > 0)
            MenuText.Wrapped(palette.Description);
    }

    private void DrawPreview(string folder, ImplPalette palette)
    {
        var buttons = ImGui.GetFrameHeightWithSpacing() * ButtonRows;
        var height = Math.Max(ImGui.GetFrameHeight(), ImGui.GetContentRegionAvail().Y - buttons);

        if (ImGui.BeginChild("preview", new Vector2(0, height), ImGuiChildFlags.None))
            DrawPictureAndColors(folder, palette);

        ImGui.EndChild();
    }

    private void DrawPictureAndColors(string folder, ImplPalette palette)
    {
        if (!DrawCharacter(folder, palette))
        {
            PaletteSwatches.DrawAllColors(palette, ImGui.GetContentRegionAvail().X);
            return;
        }

        if (ImGui.CollapsingHeader("Colors"))
            PaletteSwatches.DrawAllColors(palette, ImGui.GetContentRegionAvail().X);
    }

    private bool DrawCharacter(string folder, ImplPalette palette)
    {
        var available = ImGui.GetContentRegionAvail();
        var box = new Vector2(Step(available.X), Step(available.Y - ImGui.GetFrameHeightWithSpacing()));

        if (box.X <= 0 || box.Y <= 0)
            return false;

        if (!thumbnails.Draw(ImGui.GetWindowDrawList(), folder, palette, ImGui.GetCursorScreenPos(), box))
            return false;

        ImGui.Dummy(box);
        return true;
    }

    private static float Step(float size) => MathF.Floor(size / PreviewStep) * PreviewStep;

    private void DrawButtons(string folder, ImplPalette palette)
    {
        var width = new Vector2(RightEdge, 0);
        var slot = PlayingSlot(folder);

        ImGui.BeginDisabled(slot is null);

        if (ImGui.Button("Edit palette", width) && slot is { } playing)
            Edit(playing, palette);

        ImGui.EndDisabled();
        MenuText.Tooltip(slot is null
            ? "Palettes can be edited during a match, on a character that is playing."
            : "Puts this palette on the character in the match and opens the editor.");

        ImGui.BeginDisabled(files.IsDialogOpen);

        if (ImGui.Button("Export .impl", width))
            files.Export(palette);

        ImGui.EndDisabled();
        MenuText.Tooltip("Saves a copy of the palette file where you choose, to share it.");

        ImGui.PushStyleColor(ImGuiCol.Text, DeleteText);

        if (ImGui.Button("Delete", width))
            deletion.Ask(folder, palette);

        ImGui.PopStyleColor();
        MenuText.Tooltip("Removes the palette files from the palettes folder.");
    }

    private int? PlayingSlot(string folder)
    {
        if (!editor.InMatch)
            return null;

        foreach (var slot in PlayerSlots.InPlayerOrder)
        {
            if (editor.Character(slot) is { } character && character.Character.Folder.Equals(folder, StringComparison.OrdinalIgnoreCase))
                return slot;
        }

        return null;
    }

    private void Edit(int slot, ImplPalette palette)
    {
        editor.Wear(slot, palette);
        editorWindow.Show(slot);
    }
}
