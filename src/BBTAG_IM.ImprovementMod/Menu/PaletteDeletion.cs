using BBTAG_IM.Core.Palettes;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteDeletion(PaletteLibrary library, PaletteChoices choices, MenuSettings settings, PaletteSelection selection)
{
    private const string Prompt = "Delete palette###delete palette";

    private PaletteKey? _deleting;
    private bool _ask;

    public void Ask(string folder, ImplPalette palette)
    {
        _deleting = new PaletteKey(folder, palette.Name);
        _ask = true;
    }

    public void Draw()
    {
        if (_ask)
        {
            _ask = false;
            ImGui.OpenPopup(Prompt);
        }

        var open = true;

        if (!ImGui.BeginPopupModal(Prompt, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        if (_deleting is { } deleting)
            DrawQuestion(deleting);
        else
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }

    private void DrawQuestion(PaletteKey deleting)
    {
        ImGui.PushTextWrapPos(PromptButtons.TextWidth);
        ImGui.TextUnformatted($"Delete {deleting.Name} of {deleting.Folder}?");
        ImGui.TextDisabled($"Its files are removed from the {PaletteLibrary.FolderName} folder and its color assignment is cleared. This cannot be undone.");
        ImGui.PopTextWrapPos();

        if (PromptButtons.Button("Cancel"))
        {
            _deleting = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (!PromptButtons.DangerButton("Delete"))
            return;

        Delete(deleting);
        _deleting = null;
        ImGui.CloseCurrentPopup();
    }

    private void Delete(PaletteKey deleting)
    {
        try
        {
            var deleted = library.Delete(deleting.Folder, deleting.Name);
            settings.Save(options => options.AssignPalette(deleting.Folder, deleting.Name, null));
            choices.Rescan(deleting.Folder);
            selection.Clear();
            settings.Report(deleted == 0 ? $"{deleting.Name} had no file left to delete." : $"Deleted {deleting.Name} of {deleting.Folder}.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            settings.Report($"{deleting.Name} could not be deleted: {exception.Message}");
        }
    }

    private sealed record PaletteKey(string Folder, string Name);
}
