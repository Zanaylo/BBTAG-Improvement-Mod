using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteChoices(PaletteEditor editor, PaletteCatalog catalog, MenuSettings settings)
{
    public const string Reading = "Reading the palettes folder.";

    private IReadOnlyList<string>? _characters;

    public static string Wearing(EditedCharacter character) =>
        character.PaletteName ?? (character.IsEdited ? "edited colors" : PaletteLibrary.GameColors);

    public CharacterPalettes? Of(string folder) => catalog.LoadInBackground(folder);

    public CharacterPalettes? Loaded(string folder) => catalog.Loaded(folder);

    public IReadOnlyList<string> Characters() => _characters ??= catalog.Library.CharactersWithPalettes();

    public void Rescan(string folder)
    {
        catalog.Reload(folder);
        _characters = null;
    }

    public void Rescan()
    {
        catalog.ReloadAll();
        _characters = null;
    }

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
