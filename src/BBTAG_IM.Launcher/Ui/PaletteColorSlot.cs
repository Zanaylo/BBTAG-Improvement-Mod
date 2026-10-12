using Avalonia.Media.Imaging;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Launcher.Ui;

internal sealed record PaletteOption(string Label, ImplPalette? Palette)
{
    public const string GameColorsLabel = "Game colors";

    public static PaletteOption GameColors { get; } = new(GameColorsLabel, null);
}

internal sealed class PaletteColorSlot : ObservableObject
{
    private PaletteOption _selected;
    private Bitmap? _preview;

    public PaletteColorSlot(int number, IReadOnlyList<PaletteOption> options, PaletteOption selected)
    {
        Number = number;
        Options = options;
        _selected = selected;
        _preview = PalettePreview.Create(selected.Palette);
    }

    public event Action<PaletteColorSlot>? Changed;

    public int Number { get; }

    public string Label => $"Color {Number}";

    public IReadOnlyList<PaletteOption> Options { get; }

    public void Show(PaletteOption option)
    {
        if (_selected == option)
            return;

        _selected = option;
        Preview = PalettePreview.Create(option.Palette);
        OnPropertyChanged(nameof(Selected));
        OnPropertyChanged(nameof(Details));
    }

    public PaletteOption Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;

            Show(value);
            Changed?.Invoke(this);
        }
    }

    public Bitmap? Preview
    {
        get => _preview;
        private set => SetProperty(ref _preview, value);
    }

    public string Details => _selected.Palette is { } palette ? Describe(palette) : "The game's native colors.";

    private static string Describe(ImplPalette palette)
    {
        var author = palette.Creator.Length > 0 ? $"{palette.Name} by {palette.Creator}." : $"{palette.Name}.";
        return $"{author} {palette.Description}".Trim();
    }
}
