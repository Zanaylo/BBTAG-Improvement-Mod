using System.Numerics;
using BBTAG_IM.Core.Palettes;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteBrowser(PaletteChoices choices, MenuSettings settings, PaletteSelection selection, ColorAssignmentCombo combo, SpriteThumbnails thumbnails)
{
    private const float CellWidthInFontSizes = 7;
    private const float SwatchHeightInFontSizes = 8;
    private const float SwatchInsetInFontSizes = 0.5f;
    private const float RandomComboOffsetInFontSizes = 15;
    private const float RandomComboWidthInFontSizes = 14;
    private const float Rounding = 3;
    private const float BadgePadding = 3;
    private const float BadgeMargin = 2;
    private const float Half = 0.5f;
    private const uint BadgeBackground = 0xD2191919;
    private const string RandomHelp = "Each match picks one of this character's palettes or the game colors.";
    private const string RandomExceptGameColorsHelp = "Each match picks one of this character's palettes, never the game colors.";

    private readonly List<CharacterSection> _sections = [];
    private readonly List<ImplPalette> _matches = [];

    private IReadOnlyList<string>? _sectionsOf;

    public void Draw(string filter)
    {
        RefreshSections();

        if (_sections.Count == 0)
        {
            MenuText.Muted($"No palettes yet. Put .impl files in {PaletteLibrary.FolderName}\\<character> or import them.");
            return;
        }

        foreach (var section in _sections)
            DrawSection(section, filter);
    }

    private void RefreshSections()
    {
        var characters = choices.Characters();

        if (ReferenceEquals(characters, _sectionsOf))
            return;

        _sectionsOf = characters;
        _sections.Clear();

        foreach (var folder in characters)
            _sections.Add(new CharacterSection(folder));
    }

    private void DrawSection(CharacterSection section, string filter)
    {
        var folder = section.Folder;
        var filtering = filter.Length > 0;

        if (filtering && !Match(folder, filter))
            return;

        if (filtering)
            ImGui.SetNextItemOpen(true);

        var assigned = settings.Current.Palettes.TryGetValue(folder, out var colors) ? colors.Count : 0;

        if (!ImGui.CollapsingHeader(section.Label(choices.Loaded(folder)?.Palettes.Count, assigned)))
            return;

        ImGui.PushID(folder);
        DrawRandomRow(folder, RandomPalette.Any, ColorLabels.RandomLabel, RandomHelp);
        DrawRandomRow(folder, RandomPalette.ExceptGameColors, ColorLabels.RandomExceptGameColorsLabel, RandomExceptGameColorsHelp);
        DrawPalettes(folder, filtering);
        ImGui.PopID();
        ImGui.Spacing();
    }

    private void DrawPalettes(string folder, bool filtering)
    {
        if (choices.Of(folder) is not { } palettes)
        {
            MenuText.Muted(PaletteChoices.Reading);
            return;
        }

        DrawCells(folder, palettes, filtering ? _matches : palettes.Palettes);

        foreach (var problem in palettes.Problems)
            MenuText.Muted(problem);
    }

    private bool Match(string folder, string filter)
    {
        var everyPalette = folder.Contains(filter, StringComparison.OrdinalIgnoreCase);
        _matches.Clear();

        if (choices.Of(folder) is not { } palettes)
            return everyPalette;

        foreach (var palette in palettes.Palettes)
        {
            if (everyPalette || palette.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                _matches.Add(palette);
        }

        return _matches.Count > 0;
    }

    private void DrawRandomRow(string folder, string choice, string label, string help)
    {
        var font = ImGui.GetFontSize();

        ImGui.PushID(choice);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine();
        MenuText.HelpMarker(help);
        ImGui.SameLine(font * RandomComboOffsetInFontSizes);
        ImGui.SetNextItemWidth(font * RandomComboWidthInFontSizes);
        combo.Draw(folder, choice);
        ImGui.PopID();
    }

    private void DrawCells(string folder, CharacterPalettes source, IReadOnlyList<ImplPalette> palettes)
    {
        if (palettes.Count == 0)
        {
            MenuText.Muted("No palettes in this folder yet.");
            return;
        }

        var font = ImGui.GetFontSize();
        var cell = new Vector2(font * CellWidthInFontSizes, font * SwatchHeightInFontSizes + ImGui.GetTextLineHeightWithSpacing());
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var columns = Math.Max(1, (int)((ImGui.GetContentRegionAvail().X + spacing) / (cell.X + spacing)));

        for (var first = 0; first < palettes.Count; first += columns)
        {
            var rowStart = ImGui.GetCursorScreenPos();

            if (!ImGui.IsRectVisible(rowStart, rowStart + new Vector2(ImGui.GetContentRegionAvail().X, cell.Y)))
            {
                ImGui.Dummy(new Vector2(1, cell.Y));
                continue;
            }

            DrawRow(folder, source, palettes, first, Math.Min(first + columns, palettes.Count), cell);
        }
    }

    private void DrawRow(string folder, CharacterPalettes source, IReadOnlyList<ImplPalette> palettes, int first, int end, Vector2 cell)
    {
        for (var index = first; index < end; index++)
        {
            if (index > first)
                ImGui.SameLine();

            ImGui.PushID(index);
            DrawCell(folder, source, palettes[index], cell);
            ImGui.PopID();
        }
    }

    private void DrawCell(string folder, CharacterPalettes source, ImplPalette palette, Vector2 cell)
    {
        var topLeft = ImGui.GetCursorScreenPos();

        if (ImGui.InvisibleButton("cell", cell))
            selection.Select(folder, source, palette);

        var hovered = ImGui.IsItemHovered();
        MenuText.Tooltip(palette.Name);

        var draw = ImGui.GetWindowDrawList();
        var selected = selection.Is(folder, palette);

        if (selected || hovered)
            draw.AddRectFilled(topLeft, topLeft + cell, ImGui.GetColorU32(selected ? ImGuiCol.Header : ImGuiCol.HeaderHovered), Rounding);

        var font = ImGui.GetFontSize();
        var inset = font * SwatchInsetInFontSizes;
        var swatchHeight = font * SwatchHeightInFontSizes;
        var pictureCorner = topLeft + new Vector2(inset);
        var pictureSize = new Vector2(cell.X - inset * 2, swatchHeight - inset);

        if (!thumbnails.Draw(draw, folder, palette, pictureCorner, pictureSize))
            PaletteSwatches.DrawStripes(draw, pictureCorner, pictureSize, palette);

        DrawBadge(draw, folder, palette, topLeft, cell.X);
        DrawName(draw, palette.Name, topLeft + new Vector2(0, swatchHeight), cell.X);
    }

    private void DrawBadge(ImDrawListPtr draw, string folder, ImplPalette palette, Vector2 topLeft, float width)
    {
        if (settings.Current.ColorOf(folder, palette.Name) is not { } color)
            return;

        var text = ColorLabels.Of(color);
        var size = ImGui.CalcTextSize(text);
        var padding = new Vector2(BadgePadding, BadgeMargin);
        var corner = new Vector2(topLeft.X + width - size.X - BadgePadding * 2 - BadgeMargin, topLeft.Y + BadgeMargin);

        draw.AddRectFilled(corner, corner + size + padding * 2, BadgeBackground, Rounding);
        draw.AddText(corner + padding, ImGui.GetColorU32(ColorAssignmentCombo.AssignedText), text);
    }

    private static void DrawName(ImDrawListPtr draw, string name, Vector2 topLeft, float width)
    {
        var size = ImGui.CalcTextSize(name);
        var left = size.X < width ? topLeft.X + (width - size.X) * Half : topLeft.X;

        draw.PushClipRect(topLeft, topLeft + new Vector2(width, ImGui.GetTextLineHeightWithSpacing()), true);
        draw.AddText(new Vector2(left, topLeft.Y), ImGui.GetColorU32(ImGuiCol.Text), name);
        draw.PopClipRect();
    }

    private sealed class CharacterSection(string folder)
    {
        private const int Unknown = -1;

        private int _palettes = Unknown;
        private int _assigned = Unknown;
        private string _label = "";

        public string Folder => folder;

        public string Label(int? palettes, int assigned)
        {
            var count = palettes ?? Unknown;

            if (count == _palettes && assigned == _assigned)
                return _label;

            _palettes = count;
            _assigned = assigned;
            _label = palettes is { } known
                ? $"{folder}  ({known} {(known == 1 ? "palette" : "palettes")}, {assigned} assigned)###{folder}"
                : $"{folder}  ({assigned} assigned)###{folder}";
            return _label;
        }
    }
}
