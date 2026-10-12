using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Overlay;

public static class OverlayColors
{
    private const uint AlphaAndGreen = 0xFF00FF00;
    private const uint FirstChannel = 0x00FF0000;
    private const uint LastChannel = 0x000000FF;
    private const int ChannelDistance = 16;
    private const uint FullAlpha = 0xFF000000;
    private const int BytesPerPixel = 4;
    private const int Blue = 2;
    private const int GreenShift = 8;
    private const int BlueShift = 16;

    public static uint SwapRedAndBlue(uint color) =>
        (color & AlphaAndGreen) | ((color & FirstChannel) >> ChannelDistance) | ((color & LastChannel) << ChannelDistance);

    public static uint Opaque(uint color) => color | FullAlpha;

    public static uint Swatch(PaletteColor color) => Opaque((uint)(color.R | color.G << GreenShift | color.B << BlueShift));

    public static void ConvertRgbaToBgra(Span<byte> pixels)
    {
        for (var pixel = 0; pixel + BytesPerPixel <= pixels.Length; pixel += BytesPerPixel)
            (pixels[pixel], pixels[pixel + Blue]) = (pixels[pixel + Blue], pixels[pixel]);
    }
}
