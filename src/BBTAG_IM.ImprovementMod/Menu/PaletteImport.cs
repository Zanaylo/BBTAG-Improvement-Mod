using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Selection;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteImport(PaletteLibrary library, PaletteChoices choices, MenuSettings settings)
{
    private const string Prompt = "Import palette###import character";

    private readonly Queue<ImplPalette> _waiting = new();

    private IReadOnlyList<string> _characters = [];
    private int _character;
    private bool _ask;

    public void Files(IReadOnlyList<string> paths)
    {
        var imported = 0;
        string? problem = null;

        foreach (var path in paths)
        {
            if (Open(path, ref problem) is not { } palette)
                continue;

            if (Characters.NameOf(palette.CharacterIndex) is not { } character)
            {
                _waiting.Enqueue(palette);
                _ask = true;
                continue;
            }

            if (Into(palette, character, ref problem))
                imported++;
        }

        Report(imported, problem);
    }

    public void Draw()
    {
        if (_ask && _waiting.Count > 0)
        {
            _ask = false;
            _characters = CharacterNames();
            _character = 0;
            ImGui.OpenPopup(Prompt);
        }

        var open = true;

        if (!ImGui.BeginPopupModal(Prompt, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (!open)
                _waiting.Clear();

            return;
        }

        if (_waiting.Count == 0)
            ImGui.CloseCurrentPopup();
        else
            DrawCharacterChoice(_waiting.Peek());

        ImGui.EndPopup();
    }

    private ImplPalette? Open(string path, ref string? problem)
    {
        try
        {
            return library.Open(path);
        }
        catch (InvalidPaletteException exception)
        {
            problem = exception.Message;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            problem = $"{Path.GetFileName(path)} could not be read: {exception.Message}";
        }

        return null;
    }

    private bool Into(ImplPalette palette, string character, ref string? problem)
    {
        try
        {
            var name = library.Import(palette, character);
            choices.Rescan(character);
            settings.Report($"Imported {name} for {character}.");
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            problem = $"{palette.Name} could not be imported: {exception.Message}";
            return false;
        }
    }

    private void Report(int imported, string? problem)
    {
        if (problem is not null)
        {
            settings.Report(imported > 0 ? $"Imported {imported} palettes. {problem}" : problem);
            return;
        }

        if (imported > 1)
            settings.Report($"Imported {imported} palettes.");
    }

    private void DrawCharacterChoice(ImplPalette palette)
    {
        ImGui.PushTextWrapPos(PromptButtons.TextWidth);
        ImGui.TextUnformatted($"{palette.Name} does not say which character it is for. Pick one.");
        ImGui.PopTextWrapPos();

        if (_waiting.Count > 1)
            ImGui.TextDisabled($"{_waiting.Count - 1} more after this one.");

        DrawCharacterCombo();

        if (PromptButtons.Button("Cancel"))
        {
            _waiting.Clear();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (PromptButtons.Button("Import"))
            ImportWaiting();
    }

    private void DrawCharacterCombo()
    {
        ImGui.SetNextItemWidth(PromptButtons.TextWidth);

        if (ImGui.BeginCombo("##character", _characters[_character]))
        {
            for (var index = 0; index < _characters.Count; index++)
            {
                if (ImGui.Selectable(_characters[index], index == _character))
                    _character = index;

                MenuCombo.KeepSelectedInView(index == _character);
            }

            ImGui.EndCombo();
        }

        _character = WheelChoice.Next(_character, _characters.Count, MenuCombo.WheelSteps());
    }

    private void ImportWaiting()
    {
        string? problem = null;
        Into(_waiting.Dequeue(), _characters[_character], ref problem);

        if (problem is not null)
            settings.Report(problem);

        if (_waiting.Count == 0)
            ImGui.CloseCurrentPopup();
    }

    private IReadOnlyList<string> CharacterNames()
    {
        var modded = choices.Characters().Where(folder => Characters.IndexOf(folder) is null);
        return [.. Characters.Names, .. modded];
    }
}
