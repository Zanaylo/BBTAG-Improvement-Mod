using System.Numerics;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteAssignmentsWindow(
    MenuSettings settings,
    PaletteChoices choices,
    PaletteBrowser browser,
    PaletteDetails details,
    PaletteFiles files,
    PaletteDeletion deletion) : IMenuWindow
{
    public const string Title = "Palette assignments";

    private const uint FilterLength = 64;
    private const float FilterWidthInFontSizes = 18;
    private const float SplitterWidth = 6;
    private const float DefaultDetailsInFontSizes = 22;
    private const float NarrowestDetailsInFontSizes = 14;
    private const float NarrowestGridInFontSizes = 16;
    private const float SmallestWidthInFontSizes = 40;
    private const float SmallestHeightInFontSizes = 20;

    private static readonly Vector2 FirstPosition = new(0.15f, 0.06f);
    private static readonly Vector2 FirstSize = new(0.7f, 0.85f);

    private bool _isOpen;
    private string _filter = "";
    private float? _draggedWidth;

    public bool IsOpen => _isOpen;

    public void Toggle() => _isOpen = !_isOpen;

    public void Draw(Vector2 displaySize)
    {
        if (!_isOpen)
            return;

        var font = ImGui.GetFontSize();
        ImGui.SetNextWindowPos(displaySize * FirstPosition, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(displaySize * FirstSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(font * SmallestWidthInFontSizes, font * SmallestHeightInFontSizes), new Vector2(float.MaxValue));

        if (ImGui.Begin(Title, ref _isOpen))
            DrawContents();

        ImGui.End();
    }

    private void DrawContents()
    {
        MenuText.Muted("Picking that color in game loads the assigned palette automatically.");
        DrawToolbar();

        if (settings.Status.Length > 0)
            MenuText.Wrapped(settings.Status);

        DrawPanes();
        files.Draw();
        deletion.Draw();
    }

    private void DrawToolbar()
    {
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * FilterWidthInFontSizes);
        ImGui.InputTextWithHint("##filter", "Search palettes or characters", ref _filter, FilterLength);
        ImGui.SameLine();
        ImGui.BeginDisabled(files.IsDialogOpen);

        if (ImGui.Button("Import palettes..."))
            files.Import();

        ImGui.EndDisabled();
        ImGui.SameLine();
        MenuText.HelpMarker("Copies .impl files into the palettes folder of their character. A palette that does not say its character asks for one.");
        ImGui.SameLine();

        if (ImGui.Button("Rescan"))
            choices.Rescan();

        MenuText.Tooltip("Reads the palettes folder again, for palettes added while the game runs.");
    }

    private void DrawPanes()
    {
        var available = ImGui.GetContentRegionAvail();
        var width = DetailsWidth(available.X);

        if (ImGui.BeginChild("grid", new Vector2(available.X - width - SplitterWidth, available.Y), ImGuiChildFlags.Borders))
            browser.Draw(_filter);

        ImGui.EndChild();
        ImGui.SameLine(0, 0);
        DrawSplitter(width, available.Y);
        ImGui.SameLine(0, 0);

        if (ImGui.BeginChild("details", new Vector2(width, available.Y), ImGuiChildFlags.Borders))
            details.Draw();

        ImGui.EndChild();
    }

    private float DetailsWidth(float available)
    {
        var font = ImGui.GetFontSize();
        var narrowest = font * NarrowestDetailsInFontSizes;
        var widest = Math.Max(narrowest, available - font * NarrowestGridInFontSizes - SplitterWidth);
        var wanted = _draggedWidth ?? settings.Current.PaletteDetailWidth ?? font * DefaultDetailsInFontSizes;

        return Math.Clamp(wanted, narrowest, widest);
    }

    private void DrawSplitter(float width, float height)
    {
        ImGui.InvisibleButton("splitter", new Vector2(SplitterWidth, height));

        if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);

        if (ImGui.IsItemActive())
            _draggedWidth = width - ImGui.GetIO().MouseDelta.X;

        if (!ImGui.IsItemDeactivated() || _draggedWidth is not { } dragged)
            return;

        _draggedWidth = null;
        settings.Save(options => options.PaletteDetailWidth = MathF.Round(dragged));
    }
}
