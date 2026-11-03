using System.Numerics;
using BBTAG_IM.Core.Selection;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class AutocompleteCombo(string id)
{
    private const uint TextLength = 128;
    private const int VisibleRows = 14;
    private const int Up = -1;
    private const int Down = 1;
    private const int NoMatch = -1;
    private const float Centered = 0.5f;

    private const ImGuiWindowFlags ListFlags =
        ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings;

    private string _text = string.Empty;
    private bool _open;
    private bool _listFocused;
    private bool _typed;
    private bool _scrollToHighlight;
    private int _highlight = NoMatch;
    private Vector2 _listMin;
    private Vector2 _listMax;

    public int? Draw(string label, IReadOnlyList<string> choices, int selected, string shownWhenNothing, float width)
    {
        if (!_open)
            _text = selected >= 0 && selected < choices.Count ? choices[selected] : shownWhenNothing;

        ImGui.SetNextItemWidth(width);
        var entered = ImGui.InputTextWithHint($"{label}##{id}", "Type to search", ref _text, TextLength, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue);
        var active = ImGui.IsItemActive();
        var below = new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y);

        if (ImGui.IsItemActivated())
            Opened(selected);

        if (ImGui.IsItemEdited())
            Typed();

        if (!_open)
            return Wheel(choices, selected);

        var matching = ChoiceSearch.Matching(choices, _typed ? _text : string.Empty);

        if (active)
            MoveHighlight(matching);

        if (entered)
            return Close(matching.Contains(_highlight) ? _highlight : matching.FirstOrDefault(NoMatch), selected);

        var picked = DrawList(choices, matching, below, width);

        if (picked is not null)
            return Close(picked.Value, selected);

        _open = active || _listFocused || IsOverList();
        return null;
    }

    private void Opened(int selected)
    {
        _open = true;
        _typed = false;
        _highlight = selected;
        _scrollToHighlight = true;
    }

    private void Typed()
    {
        _typed = true;
        _highlight = NoMatch;
    }

    private static int? Wheel(IReadOnlyList<string> choices, int selected)
    {
        var steps = MenuCombo.WheelSteps();

        if (steps == 0 || choices.Count == 0)
            return null;

        var next = selected < 0 ? (steps > 0 ? 0 : choices.Count - 1) : WheelChoice.Next(selected, choices.Count, steps);
        return next == selected ? null : next;
    }

    private void MoveHighlight(IReadOnlyList<int> matching)
    {
        var steps = ImGui.IsKeyPressed(ImGuiKey.DownArrow) ? Down : ImGui.IsKeyPressed(ImGuiKey.UpArrow) ? Up : 0;

        if (steps == 0 || matching.Count == 0)
            return;

        var position = IndexOf(matching, _highlight);
        _highlight = matching[position < 0 ? 0 : WheelChoice.Next(position, matching.Count, steps)];
        _scrollToHighlight = true;
    }

    private int? DrawList(IReadOnlyList<string> choices, IReadOnlyList<int> matching, Vector2 below, float width)
    {
        var rows = Math.Clamp(matching.Count, 1, VisibleRows);
        var padding = ImGui.GetStyle().WindowPadding;
        ImGui.SetNextWindowPos(below);
        ImGui.SetNextWindowSize(new Vector2(width, rows * ImGui.GetTextLineHeightWithSpacing() + padding.Y * 2));
        int? picked = null;

        if (ImGui.Begin($"##{id}list", ListFlags))
        {
            picked = DrawRows(choices, matching);
            _listFocused = ImGui.IsWindowFocused();
            _listMin = ImGui.GetWindowPos();
            _listMax = _listMin + ImGui.GetWindowSize();
        }

        ImGui.End();
        _scrollToHighlight = false;
        return picked;
    }

    private int? DrawRows(IReadOnlyList<string> choices, IReadOnlyList<int> matching)
    {
        if (matching.Count == 0)
        {
            MenuText.Muted("Nothing matches.");
            return null;
        }

        int? picked = null;

        foreach (var index in matching)
        {
            var highlighted = index == _highlight;

            if (ImGui.Selectable($"{choices[index]}##{index}", highlighted))
                picked = index;

            if (highlighted && _scrollToHighlight)
                ImGui.SetScrollHereY(Centered);
        }

        return picked;
    }

    private bool IsOverList() => ImGui.IsMouseHoveringRect(_listMin, _listMax, clip: false);

    private int? Close(int chosen, int selected)
    {
        _open = false;
        _listFocused = false;
        _highlight = NoMatch;
        return chosen == NoMatch || chosen == selected ? null : chosen;
    }

    private static int IndexOf(IReadOnlyList<int> matching, int index)
    {
        for (var position = 0; position < matching.Count; position++)
        {
            if (matching[position] == index)
                return position;
        }

        return NoMatch;
    }
}
