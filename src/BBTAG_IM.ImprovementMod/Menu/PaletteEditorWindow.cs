using System.Numerics;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Selection;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteEditorWindow(PaletteEditor editor, PaletteChoices choices, MenuSettings settings) : IMenuWindow
{
    private const string Title = "Palette editor";
    private const int CharacterFile = 0;
    private const int FirstEffectFile = 1;
    private const int SwatchesPerRow = 16;
    private const int SwatchRows = 16;
    private const float SwatchSideInFontSizes = 1.1f;
    private const float SwatchSpacing = 2;
    private const float SwatchBorder = 2;
    private const float PickerWidthInFontSizes = 14;
    private const float FieldWidthInFontSizes = 16;
    private const float FitContent = 0;
    private const uint NameLength = 31;
    private const uint DescriptionLength = 63;
    private const float FullChannel = 255f;
    private const float Rounding = 0.5f;
    private const int FirstPickedEntry = 1;
    private const int OneColor = 0;
    private const int Gradient = 1;
    private const ImGuiColorEditFlags PickerFlags = ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview;
    private const ImGuiColorEditFlags TransparencyFlags = ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf;

    private static readonly Vector2 FirstPosition = new(0.45f, 0.08f);
    private static readonly Vector4 SelectedBorder = new(1, 1, 1, 1);
    private static readonly Vector4 ChangedBorder = new(1, 0.8f, 0.2f, 1);
    private static readonly string[] EntryLabels = EntryTexts(entry => $"Entry {entry}");
    private static readonly string[] ChangedEntryTooltips = EntryTexts(entry => $"Entry {entry}, changed");
    private static readonly string[] ChangedEntryLabels = EntryTexts(entry => $"Entry {entry} (changed)");
    private static readonly string[] PickedCountLabels = Enumerable.Range(0, PaletteColors.ColorsPerFile + 1).Select(count => $"{count} entries picked").ToArray();
    private static readonly string[] EffectNames = Enumerable.Range(0, PaletteColors.FileCount).Select(file => $"Effect {file}").ToArray();

    private readonly SlotState[] _slots = PlayerSlots.InPlayerOrder.Select(_ => new SlotState()).ToArray();

    private bool _isOpen;
    private int? _shownSlot;
    private bool _highlightOnPick = true;
    private bool _editTransparency;
    private int _fill = OneColor;

    public bool IsOpen => _isOpen;

    public void Toggle() => _isOpen = !_isOpen;

    public void Show(int slot)
    {
        _isOpen = true;
        _shownSlot = slot;
    }

    public void Draw(Vector2 displaySize)
    {
        if (!_isOpen)
            return;

        ImGui.SetNextWindowPos(displaySize * FirstPosition, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(FitContent), ImGuiCond.FirstUseEver);

        if (ImGui.Begin(Title, ref _isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            DrawContents();

        ImGui.End();
    }

    private void DrawContents()
    {
        if (!editor.InMatch)
        {
            MenuText.Muted("Colors can be changed during a match.");
            return;
        }

        if (!ImGui.BeginTabBar("players"))
            return;

        foreach (var slot in PlayerSlots.InPlayerOrder)
        {
            if (editor.Character(slot) is not { } character)
                continue;

            var flags = slot == _shownSlot ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;

            if (!NativeImGui.BeginTabItem($"P{character.Player} {character.Character.Name}##{slot}", flags))
                continue;

            ImGui.PushID(slot);
            DrawCharacter(character, _slots[slot]);
            ImGui.PopID();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
        _shownSlot = null;
    }

    private void DrawCharacter(EditedCharacter character, SlotState state)
    {
        state.Follow(character);
        MenuText.Muted($"Color {character.Color + 1}, {PaletteChoices.Wearing(character)}.");

        if (ImGui.BeginTabBar("files"))
        {
            if (ImGui.BeginTabItem("Character"))
            {
                DrawColors(character, state, CharacterFile);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Effects"))
            {
                DrawEffects(character, state);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        DrawSaving(character, state);
    }

    private void DrawEffects(EditedCharacter character, SlotState state)
    {
        var effects = Enumerable.Range(FirstEffectFile, PaletteColors.FileCount - FirstEffectFile).Where(character.HasFile).ToArray();

        if (effects.Length == 0)
        {
            MenuText.Muted("This character has no effect colors.");
            return;
        }

        if (!effects.Contains(state.EffectFile))
            state.EffectFile = effects[0];

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * FieldWidthInFontSizes / 2);

        if (ImGui.BeginCombo("Effect colors", EffectName(state.EffectFile)))
        {
            foreach (var file in effects)
            {
                if (ImGui.Selectable(EffectName(file), file == state.EffectFile))
                    state.EffectFile = file;

                MenuCombo.KeepSelectedInView(file == state.EffectFile);
            }

            ImGui.EndCombo();
        }

        var current = Array.IndexOf(effects, state.EffectFile);
        state.EffectFile = effects[WheelChoice.Next(current, effects.Length, MenuCombo.WheelSteps())];

        DrawColors(character, state, state.EffectFile);
    }

    private void DrawColors(EditedCharacter character, SlotState state, int file)
    {
        DrawSwatches(character, state, file);
        DrawPicker(character, state, file);
        DrawEditButtons(character);
    }

    private void DrawSwatches(EditedCharacter character, SlotState state, int file)
    {
        MenuText.Muted("Ctrl+click picks more entries. Shift+click picks every entry in between.");

        var side = ImGui.GetFontSize() * SwatchSideInFontSizes;
        var padding = ImGui.GetStyle().WindowPadding;
        var size = new Vector2(SwatchesPerRow * (side + SwatchSpacing), SwatchRows * (side + SwatchSpacing)) + padding * 2;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(SwatchSpacing));

        if (ImGui.BeginChild("swatches", size, ImGuiChildFlags.Borders))
        {
            for (var entry = 0; entry < PaletteColors.ColorsPerFile; entry++)
                DrawSwatch(character, state, file, entry, side);
        }

        ImGui.EndChild();
        ImGui.PopStyleVar();
    }

    private void DrawSwatch(EditedCharacter character, SlotState state, int file, int entry, float side)
    {
        var selected = state.Selection.Contains(entry);
        var changed = character.IsChanged(file, entry);
        var outlined = selected || changed;

        ImGui.PushID(entry);

        if (outlined)
        {
            ImGui.PushStyleColor(ImGuiCol.Border, selected ? SelectedBorder : ChangedBorder);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, SwatchBorder);
        }

        if (ImGui.ColorButton("##swatch", ToVector(character.Colors.Get(file, entry)), ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop | ImGuiColorEditFlags.AlphaPreviewHalf, new Vector2(side)))
            Pick(character, state, file, entry);

        if (outlined)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }

        MenuText.Tooltip(changed ? ChangedEntryTooltips[entry] : EntryLabels[entry]);

        if ((entry + 1) % SwatchesPerRow != 0)
            ImGui.SameLine();

        ImGui.PopID();
    }

    private void Pick(EditedCharacter character, SlotState state, int file, int entry)
    {
        Select(state.Selection, entry);
        state.GradientEnds = null;

        if (_highlightOnPick)
            editor.Highlight(character.Slot, file, state.Selection.Entries);
    }

    private static void Select(EntrySelection selection, int entry)
    {
        var io = ImGui.GetIO();

        if (io.KeyShift)
        {
            selection.ExtendTo(entry);
            return;
        }

        if (io.KeyCtrl)
        {
            selection.Toggle(entry);
            return;
        }

        selection.Pick(entry);
    }

    private void DrawPicker(EditedCharacter character, SlotState state, int file)
    {
        var picksSeveral = state.Selection.Count > 1;

        ImGui.TextUnformatted(PickedLabel(character, state.Selection, file));

        if (picksSeveral)
            DrawFillChoice();

        if (picksSeveral && _fill == Gradient)
            DrawGradient(character, state, file);
        else
            DrawOneColor(character, state, file);

        if (!ImGui.IsAnyItemActive())
            state.Dragging = false;
    }

    private static string PickedLabel(EditedCharacter character, EntrySelection selection, int file)
    {
        if (selection.Count > 1)
            return PickedCountLabels[selection.Count];

        var entry = selection.Focus;
        return character.IsChanged(file, entry) ? ChangedEntryLabels[entry] : EntryLabels[entry];
    }

    private void DrawFillChoice()
    {
        ImGui.RadioButton("One color", ref _fill, OneColor);
        MenuText.Tooltip("Paints every picked entry with the same color.");
        ImGui.SameLine();
        ImGui.RadioButton("Gradient", ref _fill, Gradient);
        MenuText.Tooltip("Blends the picked entries from the first color to the last, in entry order.");
    }

    private void DrawOneColor(EditedCharacter character, SlotState state, int file)
    {
        var current = character.Colors.Get(file, state.Selection.Focus);
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * PickerWidthInFontSizes);

        var picked = _editTransparency ? PickWithTransparency(current) : PickWithoutTransparency(current);

        if (picked is { } color && color != current)
            Fill(character, state, file, Enumerable.Repeat(color, state.Selection.Count));
    }

    private void DrawGradient(EditedCharacter character, SlotState state, int file)
    {
        var ends = state.GradientEndsFor(character, file);
        var fromChanged = EditGradientEnd("From", ref ends.From);
        ImGui.SameLine();
        var toChanged = EditGradientEnd("To", ref ends.To);
        ImGui.SameLine();
        var applyPressed = ImGui.Button("Apply gradient");
        MenuText.Tooltip("Fills the picked entries with this gradient.");

        if (fromChanged || toChanged || applyPressed)
            Fill(character, state, file, PaletteGradient.Between(ends.From, ends.To, state.Selection.Count));
    }

    private bool EditGradientEnd(string label, ref PaletteColor color)
    {
        var rgba = ToVector(color);
        var flags = ImGuiColorEditFlags.NoInputs | (_editTransparency ? TransparencyFlags : ImGuiColorEditFlags.NoAlpha);

        if (!ImGui.ColorEdit4(label, ref rgba, flags))
            return false;

        color = new PaletteColor(Channel(rgba.X), Channel(rgba.Y), Channel(rgba.Z), _editTransparency ? Channel(rgba.W) : color.A);
        return true;
    }

    private void Fill(EditedCharacter character, SlotState state, int file, IEnumerable<PaletteColor> colors)
    {
        if (!state.Dragging)
        {
            editor.Remember(character.Slot);
            state.Dragging = true;
        }

        foreach (var (entry, color) in state.Selection.Entries.Zip(colors))
            editor.Change(character.Slot, file, entry, KeepingTransparency(color, character.Colors.Get(file, entry)));
    }

    private PaletteColor KeepingTransparency(PaletteColor color, PaletteColor current) =>
        _editTransparency ? color : color with { A = current.A };

    private static PaletteColor? PickWithoutTransparency(PaletteColor current)
    {
        var rgb = new Vector3(current.R, current.G, current.B) / FullChannel;

        if (!ImGui.ColorPicker3("##picker", ref rgb, PickerFlags))
            return null;

        return new PaletteColor(Channel(rgb.X), Channel(rgb.Y), Channel(rgb.Z), current.A);
    }

    private static PaletteColor? PickWithTransparency(PaletteColor current)
    {
        var rgba = ToVector(current);
        if (!ImGui.ColorPicker4("##picker", ref rgba, PickerFlags | TransparencyFlags))
            return null;

        return new PaletteColor(Channel(rgba.X), Channel(rgba.Y), Channel(rgba.Z), Channel(rgba.W));
    }

    private void DrawEditButtons(EditedCharacter character)
    {
        ImGui.BeginDisabled(character.UndoCount == 0);

        if (ImGui.Button("Undo"))
            editor.Undo(character.Slot);

        ImGui.EndDisabled();
        MenuText.Tooltip($"Undo the last change. {character.UndoCount} left.");
        ImGui.SameLine();

        if (ImGui.Button("Reset"))
            editor.Reset(character.Slot);

        MenuText.Tooltip("Goes back to the game colors. Undo brings your colors back.");

        ImGui.Checkbox("Blink the picked color", ref _highlightOnPick);
        MenuText.Tooltip("Makes the picked color blink on the character for a moment, so you can see where it is used.");
        ImGui.Checkbox("Edit transparency", ref _editTransparency);
    }

    private void DrawSaving(EditedCharacter character, SlotState state)
    {
        var width = ImGui.GetFontSize() * FieldWidthInFontSizes;

        ImGui.SetNextItemWidth(width);
        ImGui.InputText("Name", ref state.Name, NameLength);

        var creator = settings.Current.PaletteCreator;
        ImGui.SetNextItemWidth(width);

        if (ImGui.InputText("Author", ref creator, NameLength))
            settings.Current.PaletteCreator = creator;

        if (ImGui.IsItemDeactivatedAfterEdit())
            settings.Save(_ => { });

        ImGui.SetNextItemWidth(width);
        ImGui.InputText("Description", ref state.Description, DescriptionLength);

        var name = state.Name.Trim();
        ImGui.BeginDisabled(!PaletteLibrary.IsUsableName(name));

        if (ImGui.Button("Save"))
            Save(character, state, name);

        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.TextDisabled($"into {PaletteLibrary.FolderName}\\{character.Character.Folder}");
    }

    private void Save(EditedCharacter character, SlotState state, string name)
    {
        try
        {
            var path = editor.Save(character.Slot, name, settings.Current.PaletteCreator.Trim(), state.Description.Trim());
            choices.Rescan(character.Character.Folder);
            choices.Choose(character, name);
            settings.Report($"Saved {Path.GetFileName(path)}. {character.Character.Name} color {character.Color + 1} wears it from now on.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            settings.Report($"The palette could not be saved: {exception.Message}");
        }
    }

    private static string EffectName(int file) => EffectNames[file];

    private static string[] EntryTexts(Func<int, string> text) => Enumerable.Range(0, PaletteColors.ColorsPerFile).Select(text).ToArray();

    private static Vector4 ToVector(PaletteColor color) => new Vector4(color.R, color.G, color.B, color.A) / FullChannel;

    private static byte Channel(float value) => (byte)Math.Clamp(value * FullChannel + Rounding, 0, FullChannel);

    private sealed class SlotState
    {
        public string Name = "";
        public string Description = "";

        private CharacterKey? _following;

        public EntrySelection Selection { get; } = new(FirstPickedEntry);

        public GradientEnds? GradientEnds { get; set; }

        public int EffectFile { get; set; } = FirstEffectFile;

        public bool Dragging { get; set; }

        public void Follow(EditedCharacter character)
        {
            var key = new CharacterKey(character.Character.Folder, character.Color);

            if (key == _following)
                return;

            _following = key;
            Name = character.PaletteName ?? "";
            Description = "";
            Dragging = false;
            GradientEnds = null;
        }

        public GradientEnds GradientEndsFor(EditedCharacter character, int file)
        {
            if (GradientEnds is { } ends && ends.File == file)
                return ends;

            GradientEnds = new GradientEnds(file, character.Colors.Get(file, Selection.Entries.First()), character.Colors.Get(file, Selection.Entries.Last()));
            return GradientEnds;
        }
    }

    private sealed class GradientEnds(int file, PaletteColor from, PaletteColor to)
    {
        public PaletteColor From = from;
        public PaletteColor To = to;

        public int File => file;
    }

    private sealed record CharacterKey(string Folder, int Color);
}
