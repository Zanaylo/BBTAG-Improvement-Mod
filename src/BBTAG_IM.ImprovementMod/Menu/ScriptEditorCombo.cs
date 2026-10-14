using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.ImprovementMod.Dev;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class ScriptEditorCombo(MenuSettings settings, ScriptEditorList editors, FileDialog dialog)
{
    private const string AskEveryTime = "Ask every time (Open with window)";
    private const string OtherProgram = "Other program...";
    private const string Searching = "Looking for programs...";

    public void Reopened() => editors.Refresh();

    public void Draw()
    {
        editors.LoadOnce();
        TakeChosenProgram();

        var saved = settings.Current.ScriptEditor;
        var choices = editors.Choices;
        var open = ImGui.BeginCombo("Open scripts with", saved is null ? AskEveryTime : ScriptEditorChoices.NameOf(saved, choices));
        MenuText.Tooltip(saved ?? "The Windows Open with window asks which program opens the script.");

        if (!open)
            return;

        if (ImGui.Selectable(AskEveryTime, saved is null))
            Choose(null);

        MenuCombo.KeepSelectedInView(saved is null);
        DrawChoices(choices, saved);

        if (editors.IsLoading)
            MenuText.Muted(Searching);

        if (ImGui.Selectable(OtherProgram) && !dialog.IsOpen)
            dialog.ChooseProgram();

        ImGui.EndCombo();
    }

    private void DrawChoices(IReadOnlyList<ScriptEditorChoice> choices, string? saved)
    {
        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            var selected = choice.Program.Equals(saved, StringComparison.OrdinalIgnoreCase);

            ImGui.PushID(index);

            if (ImGui.Selectable(choice.Name, selected))
                Choose(choice.Program);

            MenuText.Tooltip(choice.Program);
            MenuCombo.KeepSelectedInView(selected);
            ImGui.PopID();
        }
    }

    private void TakeChosenProgram()
    {
        if (dialog.Take() is { Paths: [var program, ..] })
            Choose(program);
    }

    private void Choose(string? program) =>
        settings.Save(
            options => options.ScriptEditor = program,
            program is null ? "Scripts ask for a program each time they open." : $"Scripts open in {Path.GetFileNameWithoutExtension(program)}.");
}
