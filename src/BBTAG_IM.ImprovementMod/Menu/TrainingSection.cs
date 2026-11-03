using System.Numerics;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.Core.Selection;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.ImprovementMod.Training;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class TrainingSection(MenuSettings settings, PauseControls pause, BossFightControl? bossFight) : IMenuSection
{
    public const string Name = "Training";

    private const int TypeColumns = 4;
    private const float Transparent = 0;
    private const float Opaque = 1;
    private const string NoBossLabel = "Off";

    public string Title => Name;

    public void Reopened()
    {
    }

    public void Draw()
    {
        var hitboxViewer = settings.Current.HitboxViewer;

        if (ImGui.Checkbox("Hitbox viewer", ref hitboxViewer))
            settings.Save(options => options.HitboxViewer = hitboxViewer);

        ImGui.SameLine();
        ImGui.TextDisabled($"({Hotkeys.NameOf(Hotkey.Hitboxes)})");
        MenuText.Tooltip("Shows during training matches only.");

        ImGui.Spacing();
        pause.Draw();
        DrawBoxTypes();
        ImGui.Spacing();
        DrawBossFight();
    }

    private void DrawBossFight()
    {
        if (bossFight is null)
            return;

        Boss?[] choices = [null, .. bossFight.Bosses];
        var current = Array.IndexOf(choices, bossFight.Chosen);

        if (ImGui.BeginCombo("Boss fight", NameOf(choices[current])))
        {
            for (var choice = 0; choice < choices.Length; choice++)
                DrawBossChoice(bossFight, choices[choice], choice == current);

            ImGui.EndCombo();
        }

        var next = WheelChoice.Next(current, choices.Length, MenuCombo.WheelSteps());

        if (next != current)
            bossFight.Choose(choices[next]);

        MenuText.Tooltip("Needs the overhaul with its table turned on. Pick a boss, then confirm any team for Player 2 and any stage on character select. Player 2 becomes the boss alone and the match is played on the boss stage. This is a test and can still crash. It turns off when the game closes.");

        if (bossFight.NotReady is { } notReady)
            MenuText.Muted(notReady);
    }

    private static void DrawBossChoice(BossFightControl control, Boss? boss, bool selected)
    {
        if (ImGui.Selectable(NameOf(boss), selected) && !selected)
            control.Choose(boss);

        MenuCombo.KeepSelectedInView(selected);
    }

    private static string NameOf(Boss? boss) => boss?.Name ?? NoBossLabel;

    private void DrawBoxTypes()
    {
        if (!ImGui.TreeNode("Hitbox types"))
            return;

        var origins = settings.Current.HitboxOrigins;

        if (ImGui.Checkbox("Show origin", ref origins))
            settings.Save(options => options.HitboxOrigins = origins);

        MenuText.Tooltip("Draws a cross at each object's position, the point its boxes are measured from. Use it to tell if boxes belong to a projectile or to the character who fired it.");

        if (ImGui.BeginTable("types", TypeColumns, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("On", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Fill", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Outline", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            DrawBoxType(BoxKind.Hurtbox, "Hurtbox", "Where the character can be hit. A hit happens when a hitbox overlaps one, so a move with none of these is strike invulnerable.");
            DrawBoxType(BoxKind.Hitbox, "Hitbox", "The attack itself. Out only on the frames the attack can connect.");

            ImGui.EndTable();
        }

        ImGui.TreePop();
    }

    private void DrawBoxType(BoxKind kind, string name, string description)
    {
        var style = settings.Current.StyleOf(kind);
        var swatch = ImGui.GetFontSize();

        ImGui.PushID(name);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.ColorButton("##swatch", ImGui.ColorConvertU32ToFloat4(OverlayColors.SwapRedAndBlue(HitboxColors.Of(kind))), ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, new Vector2(swatch));
        ImGui.SameLine();
        ImGui.TextUnformatted(name);
        MenuText.Tooltip(description);

        ImGui.TableNextColumn();
        var shown = style.Shown;

        if (ImGui.Checkbox("##on", ref shown))
            settings.Save(_ => style.Shown = shown);

        ImGui.TableNextColumn();
        DrawAlpha("##fill", style, style.Fill, (target, value) => target.Fill = value);

        ImGui.TableNextColumn();
        DrawAlpha("##outline", style, style.Outline, (target, value) => target.Outline = value);

        ImGui.PopID();
    }

    private void DrawAlpha(string label, BoxStyle style, float value, Action<BoxStyle, float> apply)
    {
        ImGui.SetNextItemWidth(-float.Epsilon);

        if (ImGui.SliderFloat(label, ref value, Transparent, Opaque, "%.2f"))
            apply(style, value);

        if (ImGui.IsItemDeactivatedAfterEdit())
            settings.Save(_ => { });
    }
}
