using BBTAG_IM.Core.Scripting;
using BBTAG_IM.ImprovementMod.Dev;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class ScriptLanguageChoice(MenuSettings settings, DevScripts scripts)
{
    private const string Label = "Write scripts in";
    private const string LayoutLabel = "Python layout";

    private static readonly (ScriptStyle Layout, string Name, string Description)[] Layouts =
    [
        (ScriptStyle.Mango, "Mango", "Nested like the 2019 BBTAG parser of the Team Blue tools: upon_IMMEDIATE, SLOT_56 = 1, if (not SLOT_21):."),
        (ScriptStyle.Readable, "Readable", "The previous layout, with the upon, SLOT and input names of the bbscript folder."),
    ];

    private string _notice = string.Empty;

    public void Draw()
    {
        var saved = settings.Current.ScriptLanguage;

        ImGui.Text(Label);
        ImGui.SameLine();
        DrawOption("C#", ScriptLanguage.CSharp, saved);
        ImGui.SameLine();
        DrawOption("Python", ScriptLanguage.Python, saved);
        MenuText.Tooltip("Scripts that are being edited keep their language until the match loads again.");

        if (saved == ScriptLanguage.Python)
            DrawLayouts(settings.Current.PythonLayout);

        if (_notice.Length > 0)
            MenuText.Wrapped(_notice);
    }

    private void DrawOption(string name, ScriptLanguage language, ScriptLanguage saved)
    {
        if (!ImGui.RadioButton(name, saved == language) || saved == language)
            return;

        settings.Save(options => options.ScriptLanguage = language, $"New scripts open as {name} files.");
    }

    private void DrawLayouts(ScriptStyle saved)
    {
        var canChange = scripts.CanRewriteScripts;
        var current = Layouts.FirstOrDefault(layout => layout.Layout == saved);
        ImGui.BeginDisabled(!canChange);

        if (ImGui.BeginCombo(LayoutLabel, current.Name ?? saved.ToString()))
        {
            foreach (var (layout, name, description) in Layouts)
            {
                var selected = layout == saved;

                if (ImGui.Selectable(name, selected) && !selected)
                    Choose(layout);

                MenuText.Tooltip(description);
                MenuCombo.KeepSelectedInView(selected);
            }

            ImGui.EndCombo();
        }

        ImGui.EndDisabled();
        MenuText.Tooltip(canChange
            ? $"{current.Description} Changing it writes the saved .py files of the DEV folder again, with a copy of the old ones in DEV\\.backup. Both layouts build the same script."
            : "Go back to character select to change it. Scripts being edited keep their layout until the match loads again.");
    }

    private void Choose(ScriptStyle layout)
    {
        settings.Save(options => options.PythonLayout = layout);
        _notice = scripts.UsePythonLayout(layout);
    }
}
