using BBTAG_IM.Core.Scripting;
using BBTAG_IM.ImprovementMod.Dev;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class CommandNamesCombo(DevScripts scripts, FileDialog dialog)
{
    private const string Label = "Command names";
    private const string AddFile = "Add a command list...";
    private const string NoList = "None (numbers only)";

    private string _notice = string.Empty;

    public void Draw()
    {
        TakeChosenFile();

        var choice = scripts.CommandChoice;
        var current = choice.CurrentPath;
        var files = choice.Available();
        var shown = files.FirstOrDefault(file => IsSame(file.Path, current))?.Name ?? NoList;

        var canChange = scripts.CanRewriteScripts;
        ImGui.BeginDisabled(!canChange);

        if (ImGui.BeginCombo(Label, shown))
        {
            DrawFiles(files, current);

            if (ImGui.Selectable(AddFile) && !dialog.IsOpen)
                dialog.ChooseCommandDatabase();

            ImGui.EndCombo();
        }

        ImGui.EndDisabled();
        MenuText.Tooltip(canChange
            ? $"The command list (commandDB.json) that names the commands in C# and Python. Lists in {Path.Combine("bbscript", CommandDatabaseChoice.LibraryFolder)} show here. Saved scripts in the DEV folder are rewritten with the new names, and a copy of the old ones stays in DEV\\.backup."
            : "Go back to character select to change it. Scripts being edited keep their names until the match loads again.");

        if (_notice.Length > 0)
            MenuText.Wrapped(_notice);
    }

    private void DrawFiles(IReadOnlyList<CommandDatabaseFile> files, string current)
    {
        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            var selected = IsSame(file.Path, current);

            ImGui.PushID(index);

            if (ImGui.Selectable(file.Name, selected) && !selected)
                _notice = scripts.UseCommands(file.Path);

            MenuText.Tooltip(file.Path);
            MenuCombo.KeepSelectedInView(selected);
            ImGui.PopID();
        }
    }

    private void TakeChosenFile()
    {
        if (dialog.Take() is { Paths: [var path, ..] })
            _notice = scripts.AddAndUseCommands(path);
    }

    private static bool IsSame(string path, string other) => string.Equals(path, other, StringComparison.OrdinalIgnoreCase);
}
