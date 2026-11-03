using BBTAG_IM.Core.Scripting;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class ScriptLanguageChoice(MenuSettings settings)
{
    private const string Label = "Write scripts in";

    public void Draw()
    {
        var saved = settings.Current.ScriptLanguage;

        ImGui.Text(Label);
        ImGui.SameLine();
        DrawOption("C#", ScriptLanguage.CSharp, saved);
        ImGui.SameLine();
        DrawOption("Python", ScriptLanguage.Python, saved);
        MenuText.Tooltip("Scripts that are being edited keep their language until the match loads again.");
    }

    private void DrawOption(string name, ScriptLanguage language, ScriptLanguage saved)
    {
        if (!ImGui.RadioButton(name, saved == language) || saved == language)
            return;

        settings.Save(options => options.ScriptLanguage = language, $"New scripts open as {name} files.");
    }
}
