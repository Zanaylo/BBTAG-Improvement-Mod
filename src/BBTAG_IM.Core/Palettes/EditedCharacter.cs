using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Palettes;

public sealed class EditedCharacter
{
    public const int UndoDepth = 32;

    private const int PlayerCount = 2;

    private readonly List<PaletteColors> _history = [];
    private readonly bool[] _files;

    internal EditedCharacter(int slot, CharacterIdentity character, int color, uint fileList, PaletteColors original, PaletteColors partnerOriginal, bool[] files)
    {
        Slot = slot;
        Character = character;
        Color = color;
        FileList = fileList;
        Original = original;
        PartnerOriginal = partnerOriginal;
        Colors = original.Copy();
        _files = files;
    }

    public int Slot { get; }

    public int Player => Slot % PlayerCount + 1;

    public CharacterIdentity Character { get; }

    public int Color { get; }

    public int Partner => PaletteTable.PartnerOf(Color);

    public PaletteColors Original { get; }

    public PaletteColors Colors { get; private set; }

    public string? PaletteName { get; private set; }

    public int UndoCount => _history.Count;

    public bool IsEdited => !Colors.SameAs(Original);

    internal uint FileList { get; }

    internal PaletteColors PartnerOriginal { get; }

    internal bool NeedsRedraw { get; set; }

    internal bool Redrawn { get; set; }

    internal PaletteColors? LastShown { get; set; }

    public bool HasFile(int file) => _files[file];

    public bool IsChanged(int file, int entry) => Colors.Get(file, entry) != Original.Get(file, entry);

    internal void Remember()
    {
        if (_history.Count == UndoDepth)
            _history.RemoveAt(0);

        _history.Add(Colors.Copy());
    }

    internal void Undo()
    {
        if (_history.Count == 0)
            return;

        Colors = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        NeedsRedraw = true;
    }

    internal void Change(int file, int entry, PaletteColor color)
    {
        Colors.Set(file, entry, color);
        NeedsRedraw = true;
    }

    internal void Name(string paletteName) => PaletteName = paletteName;

    internal void Wear(PaletteColors colors, string? paletteName)
    {
        Colors = colors;
        PaletteName = paletteName;
        NeedsRedraw = true;
    }
}
