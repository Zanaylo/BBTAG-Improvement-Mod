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

    private static readonly Vector2 FirstPosition = new(0.45f, 0.08f);
    private static readonly Vector4 SelectedBorder = new(1, 1, 1, 1);
    private static readonly Vector4 ChangedBorder = new(1, 0.8f, 0.2f, 1);
    private static readonly string[] EntryLabels = EntryTexts(entry => $"Entry {entry}");
    private static readonly string[] ChangedEntryTooltips = EntryTexts(entry => $"Entry {entry}, changed");
    private static readonly string[] ChangedEntryLabels = EntryTexts(entry => $"Entry {entry} (changed)");
    private static readonly string[] EffectNames = Enumerable.Range(0, PaletteColors.FileCount).Select(file => $"Effect {file}").ToArray();

    private readonly SlotState[] _slots = PlayerSlots.InPlayerOrder.Select(_ => new SlotState()).ToArray();

    private bool _isOpen;
    private int? _shownSlot;
    private bool _highlightOnPick = true;
    private bool _editTransparency;

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
        var selected = entry == state.Entry;
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
        state.Entry = entry;

        if (_highlightOnPick)
            editor.Highlight(character.Slot, file, entry);
    }

    private void DrawPicker(EditedCharacter character, SlotState state, int file)
    {
        var entry = state.Entry;
        var current = character.Colors.Get(file, entry);

        ImGui.TextUnformatted(character.IsChanged(file, entry) ? ChangedEntryLabels[entry] : EntryLabels[entry]);
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * PickerWidthInFontSizes);

        var picked = _editTransparency ? PickWithTransparency(current) : PickWithoutTransparency(current);

        if (picked is { } color && color != current)
            ChangeColor(character, state, file, entry, color);

        if (!ImGui.IsAnyItemActive())
            state.Dragging = false;
    }

    private void ChangeColor(EditedCharacter character, SlotState state, int file, int entry, PaletteColor color)
    {
        if (!state.Dragging)
        {
            editor.Remember(character.Slot);
            state.Dragging = true;
        }

        editor.Change(character.Slot, file, entry, color);
    }

    private static PaletteColor? PickWithoutTransparency(PaletteColor current)
    {
        var rgb = new Vector3(current.R, current.G, current.B) / FullChannel;

        if (!ImGui.ColorPicker3("##picker", ref rgb, ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview))
            return null;

        return new PaletteColor(Channel(rgb.X), Channel(rgb.Y), Channel(rgb.Z), current.A);
    }

    private static PaletteColor? PickWithTransparency(PaletteColor current)
    {
        var rgba = ToVector(current);
        var flags = ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf;

        if (!ImGui.ColorPicker4("##picker", ref rgba, flags))
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

        public int Entry { get; set; } = 1;

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
        }
    }

    private sealed record CharacterKey(string Folder, int Color);
}
