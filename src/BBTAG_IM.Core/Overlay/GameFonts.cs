using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Overlay;

public sealed record GameFontSource(string Archive, string FileName, GlyphGrid Grid);

public static class GameFonts
{
    public static readonly GameFontSource Interface = new(
        @"gr\gr_johchu_cmn.pac",
        "bb4font0.hip",
        new GlyphGrid(75, 10, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!\"#$%&,()*+-./:;<=>?@[\\]^_`{|}~"));

    public static GlyphSheet? Load(GameArchiveFile files, GameFontSource source)
    {
        if (files.Read(source.Archive, source.FileName) is not { } hip)
            return null;

        try
        {
            return GlyphSheet.Measure(ColorHip.Decode(hip), source.Grid);
        }
        catch (InvalidSpriteDataException)
        {
            return null;
        }
    }
}
