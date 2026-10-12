using System.Numerics;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Selection;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class ColorAssignmentCombo(MenuSettings settings)
{
    public static readonly Vector4 AssignedText = new(0.30f, 0.85f, 0.39f, 1);

    private const int NotAssignedIndex = 0;

    public void Draw(string folder, string assignment)
    {
        var assigned = settings.Current.ColorOf(folder, assignment);
        var colors = Math.Max(PaletteTable.StockColors, settings.Current.HighestColor(folder));

        if (assigned is not null)
            ImGui.PushStyleColor(ImGuiCol.Text, AssignedText);

        var open = ImGui.BeginCombo("##color", assigned is { } color ? ColorLabels.Of(color) : ColorLabels.NotAssigned);

        if (assigned is not null)
            ImGui.PopStyleColor();

        if (open)
        {
            DrawChoices(folder, assignment, assigned, colors);
            ImGui.EndCombo();
        }

        AssignByWheel(folder, assignment, assigned, colors);
    }

    private void DrawChoices(string folder, string assignment, int? assigned, int colors)
    {
        if (ImGui.Selectable(ColorLabels.NotAssigned, assigned is null))
            Assign(folder, assignment, null);

        MenuCombo.KeepSelectedInView(assigned is null);

        for (var color = 1; color <= colors; color++)
        {
            var selected = assigned == color;

            if (ImGui.Selectable(Label(folder, assignment, color), selected))
                Assign(folder, assignment, color);

            MenuCombo.KeepSelectedInView(selected);
        }
    }

    private string Label(string folder, string assignment, int color)
    {
        var occupant = settings.Current.PaletteFor(folder, color);

        if (occupant is null || occupant.Equals(assignment, StringComparison.OrdinalIgnoreCase))
            return ColorLabels.Of(color);

        return $"{ColorLabels.Of(color)} (used by {ColorLabels.Shown(occupant)})";
    }

    private void AssignByWheel(string folder, string assignment, int? assigned, int colors)
    {
        var steps = MenuCombo.WheelSteps();

        if (steps == 0)
            return;

        var current = assigned ?? NotAssignedIndex;
        var next = WheelChoice.Next(current, colors + 1, steps);

        if (next != current)
            Assign(folder, assignment, next == NotAssignedIndex ? null : next);
    }

    private void Assign(string folder, string assignment, int? color)
    {
        var shown = ColorLabels.Shown(assignment);

        settings.Save(
            options => options.AssignPalette(folder, assignment, color),
            color is { } chosen
                ? $"{folder} color {chosen} loads {shown} from now on."
                : $"{shown} is no longer assigned to a color of {folder}.");
    }
}
