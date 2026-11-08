using System.Numerics;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.ImprovementMod.Dev;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class DevSection(DevScripts scripts, MatchExit exit, ScriptEditorCombo editor, ScriptLanguageChoice language, HitboxEditorWindow? hitboxes, IReadOnlyList<IDevPanel> panels, string databaseFolder) : IMenuSection
{
    public const string Name = "Dev";

    private static readonly Vector4 ErrorColor = new(1f, 0.45f, 0.4f, 1f);
    private static readonly Vector4 AppliedColor = new(0.5f, 0.9f, 0.5f, 1f);

    private string _notice = string.Empty;

    public string Title => Name;

    public void Reopened() => editor.Reopened();

    public void Draw()
    {
        DrawHitboxes();
        ImGui.Spacing();
        ImGui.SeparatorText("Scripts");
        MenuText.Wrapped("Edit a character's script while the match runs. Save the file and the change applies right away.");
        MenuText.Muted($"Files go to {scripts.Folder.Path}");

        if (!scripts.HasCommandNames)
            MenuText.Muted($"Commands show as numbers. Put the BBTAG command lists (command_db.json and its folders) in {databaseFolder} to see their names.");

        if (!scripts.DevFolderLoads)
            MenuText.Colored(ErrorColor, "The DEV folder is turned off in the Mod Loader, so every match starts with the original scripts. Your saved edits are applied again a moment later, but code that only runs when the match starts (like Move_Register in MatchInit) misses them. Turn DEV on in the Mod Loader and start the game again.");

        ImGui.Spacing();
        language.Draw();
        editor.Draw();
        ImGui.Spacing();
        DrawCharacters();
        DrawSessions();
        DrawReload();

        if (_notice.Length > 0)
            MenuText.Wrapped(_notice);

        foreach (var panel in panels)
        {
            ImGui.Spacing();
            panel.Draw();
        }
    }

    private void DrawHitboxes()
    {
        ImGui.SeparatorText("Hitboxes");

        if (hitboxes is null)
        {
            MenuText.Muted("Box editing is not available in this game version.");
            return;
        }

        MenuText.Wrapped("Change the hurtboxes and hitboxes of any move or effect in a separate window. It follows the animation or pauses the match on the frame you pick.");

        if (ImGui.Button("Open the hitbox editor"))
            hitboxes.Show();
    }

    private void DrawCharacters()
    {
        var characters = scripts.Characters();

        if (characters.Count == 0)
        {
            MenuText.Muted("Start a match to edit its characters.");
            return;
        }

        foreach (var character in characters)
        {
            ImGui.PushID(character.Slot);
            ImGui.Text($"{DevScripts.SlotName(character.Slot)}: {character.Name}");
            ImGui.SameLine();

            if (ImGui.SmallButton("Script"))
                _notice = scripts.Open(character, effects: false);

            ImGui.SameLine();

            if (ImGui.SmallButton("Effects script"))
                _notice = scripts.Open(character, effects: true);

            ImGui.PopID();
        }
    }

    private void DrawSessions()
    {
        if (scripts.Sessions.Count == 0)
            return;

        ImGui.Spacing();
        ImGui.SeparatorText("Being edited");

        foreach (var session in scripts.Sessions)
        {
            ImGui.Text(Path.GetFileName(session.Paths.Source));
            ImGui.SameLine();
            MenuText.Colored(ColorOf(session.Status.State), session.Status.Message);
        }

        if (scripts.IsFrozen)
            MenuText.Colored(ErrorColor, "The match is frozen until the script is fixed. Save the fix and it goes on by itself (F5 lets it go on now).");
    }

    private void DrawReload()
    {
        ImGui.Spacing();

        if (ImGui.Button("Back to character select"))
            _notice = exit.ToCharacterSelect() ? "Pick the characters again to load every file again, the DEV folder included." : "Start a match first.";

        MenuText.Tooltip("Loads sprites, sounds and other files again after you change them in the DEV folder. Script edits do not need this.");
    }

    private static Vector4 ColorOf(ScriptSessionState state) => state switch
    {
        ScriptSessionState.CompileFailed or ScriptSessionState.RolledBack => ErrorColor,
        ScriptSessionState.Applied => AppliedColor,
        _ => ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled],
    };
}
