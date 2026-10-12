using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Launcher.Ui;

internal static unsafe class PalettePreview
{
    private const int Side = 16;
    private const double Dpi = 96;
    private const uint Opaque = 0xFF000000;

    public static Bitmap? Create(ImplPalette? palette)
    {
        if (palette is null)
            return null;

        var colors = palette.CharacterColors();
        var bitmap = new WriteableBitmap(new PixelSize(Side, Side), new Vector(Dpi, Dpi), PixelFormat.Bgra8888, AlphaFormat.Opaque);

        using var buffer = bitmap.Lock();

        for (var index = 0; index < colors.Count; index++)
        {
            var row = (uint*)(buffer.Address + index / Side * buffer.RowBytes);
            row[index % Side] = colors[index] | Opaque;
        }

        return bitmap;
    }
}
