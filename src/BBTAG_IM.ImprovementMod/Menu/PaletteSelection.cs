using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class PaletteSelection(PaletteChoices choices)
{
    private CharacterPalettes? _source;
    private ImplPalette? _palette;

    public string Folder { get; private set; } = "";

    public string Name { get; private set; } = "";

    public bool Is(string folder, ImplPalette palette) =>
        Folder.Equals(folder, StringComparison.OrdinalIgnoreCase) && Name.Equals(palette.Name, StringComparison.OrdinalIgnoreCase);

    public void Select(string folder, CharacterPalettes source, ImplPalette palette)
    {
        Folder = folder;
        Name = palette.Name;
        _source = source;
        _palette = palette;
    }

    public void Clear()
    {
        Folder = "";
        Name = "";
        _source = null;
        _palette = null;
    }

    public ImplPalette? Current()
    {
        if (Folder.Length == 0)
            return null;

        if (choices.Of(Folder) is not { } palettes || ReferenceEquals(palettes, _source))
            return _palette;

        _source = palettes;
        _palette = palettes.Find(Name);
        return _palette;
    }
}
