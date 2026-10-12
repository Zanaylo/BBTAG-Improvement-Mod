using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteChoices(PaletteEditor editor, PaletteLibrary library, MenuSettings settings)
{
    private readonly Dictionary<string, CharacterPalettes> _palettes = new(StringComparer.OrdinalIgnoreCase);

    public static string Wearing(EditedCharacter character) =>
        character.PaletteName ?? (character.IsEdited ? "edited colors" : PaletteLibrary.GameColors);

    public CharacterPalettes Of(string folder)
    {
        if (!_palettes.TryGetValue(folder, out var palettes))
            _palettes[folder] = palettes = library.Load(folder);

        return palettes;
    }

    public void Rescan(string folder) => _palettes.Remove(folder);

    public void Rescan() => _palettes.Clear();

    public void Wear(EditedCharacter character, ImplPalette? palette)
    {
        editor.Wear(character.Slot, palette);
        Choose(character, palette?.Name);
    }

    public void Choose(EditedCharacter character, string? paletteName)
    {
        var folder = character.Character.Folder;
        var color = character.Color + 1;

        settings.Save(
            options => options.ChoosePalette(folder, color, paletteName),
            paletteName is null
                ? $"{character.Character.Name} color {color} wears the game colors."
                : $"{character.Character.Name} color {color} wears {paletteName} from now on.");
    }
}
