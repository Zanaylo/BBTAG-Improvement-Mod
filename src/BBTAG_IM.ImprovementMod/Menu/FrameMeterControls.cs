using BBTAG_IM.Core.Settings;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class FrameMeterControls(MenuSettings settings, FrameMeterGuideWindow guide)
{
    private const float SmallestOffset = -0.5f;
    private const float LargestOffset = 0.5f;
    private const int StopWidthInFontSizes = 5;

    public void Draw()
    {
        var meter = settings.Current.FrameMeter;
        var shown = meter.Shown;

        if (ImGui.Checkbox("Show frame meter", ref shown))
            settings.Save(options => options.FrameMeter.Shown = shown);

        ImGui.SameLine();
        ImGui.TextDisabled($"({Hotkeys.NameOf(Hotkey.FrameMeter)})");
        MenuText.Tooltip("Shows every frame of both teams during training matches: startup, active and recovery of the point character and of the partner, hitstun, blockstun, invincibility, frame advantage and combo damage.");
        ImGui.SameLine();

        if (ImGui.SmallButton("Guide"))
            guide.Toggle();

        DrawLook(meter);
        DrawAutoPause(meter.AutoPause);
    }

    private void DrawLook(FrameMeterOptions meter)
    {
        if (!ImGui.TreeNode("Options"))
            return;

        Slider("Size", meter.Scale, FrameMeterOptions.SmallestScale, FrameMeterOptions.LargestScale, "%.2fx", value => meter.Scale = value);
        Slider("Opacity", meter.Opacity, FrameMeterOptions.FaintestOpacity, 1f, "%.2f", value => meter.Opacity = value);
        Toggle("Frame counts", meter.Counts, value => meter.Counts = value, "Writes how many frames each startup, active or recovery run lasts inside the meter.");
        Toggle("Invincibility strip", meter.Invulnerability, value => meter.Invulnerability = value, "A thin strip next to the point lane: white fully invincible, cyan strike only, pink throw only, orange projectile only, gold armor.");
        Toggle("Partner lanes", meter.Partners, value => meter.Partners = value, "A thin lane under each point character with the partner's startup, active and recovery while it is on screen.");
        Toggle("Combo line", meter.Totals, value => meter.Totals = value, "Hits, damage, blocked hits, chip, blockstring gaps and freeze frames between the two teams.");
        Slider("Move left / right", meter.OffsetX, SmallestOffset, LargestOffset, "%.2f", value => meter.OffsetX = value);
        Slider("Move up / down", meter.OffsetY, SmallestOffset, LargestOffset, "%.2f", value => meter.OffsetY = value);

        if (ImGui.SmallButton("Back to the bottom center"))
            settings.Save(options => (options.FrameMeter.OffsetX, options.FrameMeter.OffsetY) = (0, 0));

        ImGui.TreePop();
    }

    private void DrawAutoPause(AutoPauseOptions pause)
    {
        if (!ImGui.TreeNode("Auto pause"))
            return;

        MenuText.Muted("Pauses the match like F5 when one of these happens. F6 then plays one frame.");
        Toggle("Player 1", pause.Player1, value => pause.Player1 = value, "Watch what Player 1's team does.");
        ImGui.SameLine();
        Toggle("Player 2", pause.Player2, value => pause.Player2 = value, "Watch what Player 2's team does.");
        Toggle("When a move starts", pause.OnMoveStart, value => pause.OnMoveStart = value, "Pauses on the first startup frame of the point character's move.");
        Toggle("When the first hit lands", pause.OnHit, value => pause.OnHit = value, "Pauses on the frame a combo's first hit connects.");
        Toggle("When a combo reaches", pause.OnCombo, value => pause.OnCombo = value, "Pauses when the combo reaches any of the hit counts below.");
        DrawStops("combo", pause.ComboStops);
        Toggle("When blocked hits reach", pause.OnBlock, value => pause.OnBlock = value, "Pauses when a blockstring reaches any of the counts below.");
        DrawStops("block", pause.BlockStops);
        ImGui.TreePop();
    }

    private void DrawStops(string id, List<int> stops)
    {
        ImGui.PushID(id);
        ImGui.Indent();

        for (var index = 0; index < stops.Count; index++)
        {
            ImGui.PushID(index);
            var hits = stops[index];
            ImGui.SetNextItemWidth(ImGui.GetFontSize() * StopWidthInFontSizes);

            if (ImGui.InputInt("hits", ref hits))
                settings.Save(_ => stops[index] = Math.Clamp(hits, 1, AutoPauseOptions.MostHits));

            ImGui.SameLine();

            if (ImGui.SmallButton("Remove"))
            {
                var removed = index;
                settings.Save(_ => stops.RemoveAt(removed));
            }

            ImGui.PopID();
        }

        if (stops.Count < AutoPauseOptions.MostStops && ImGui.SmallButton("Add a count"))
            settings.Save(_ => stops.Add(Math.Min((stops.Count == 0 ? 0 : stops.Max()) + 1, AutoPauseOptions.MostHits)));

        ImGui.Unindent();
        ImGui.PopID();
    }

    private void Toggle(string label, bool value, Action<bool> apply, string tooltip)
    {
        if (ImGui.Checkbox(label, ref value))
            settings.Save(_ => apply(value));

        MenuText.Tooltip(tooltip);
    }

    private void Slider(string label, float value, float lowest, float highest, string format, Action<float> apply)
    {
        if (ImGui.SliderFloat(label, ref value, lowest, highest, format))
            apply(value);

        if (ImGui.IsItemDeactivatedAfterEdit())
            settings.Save(_ => { });
    }
}
