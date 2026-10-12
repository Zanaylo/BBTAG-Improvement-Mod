namespace BBTAG_IM.Launcher.Ui;

internal sealed class CharacterPaletteChoices(string name, Func<IReadOnlyList<PaletteColorSlot>> createColors)
{
    private IReadOnlyList<PaletteColorSlot>? _colors;

    public string Name => name;

    public IReadOnlyList<PaletteColorSlot> Colors => _colors ??= createColors();

    public IReadOnlyList<PaletteColorSlot> LoadedColors => _colors ?? [];
}
