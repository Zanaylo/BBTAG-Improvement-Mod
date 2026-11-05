using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Buttons;

public static class ButtonIcons
{
    public const int CellSize = 63;
    public const int Columns = 12;
    public const string ETag = "CMDS";
    public const int EIcon = 0x172;

    private const int BytesPerPixel = 4;
    private const string ECellResource = "BBTAG_IM.Core.Buttons.Data.e_icon.bgra";
    private const string AdsArchive = "fhd_ads.pac";
    private const string Atlas = "icon.hip";

    public static byte[] ECell()
    {
        using var stream = typeof(ButtonIcons).Assembly.GetManifestResourceStream(ECellResource)
            ?? throw new InvalidSpriteDataException($"{ECellResource} is missing from the mod.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return bytes.ToArray();
    }

    public static ColorImage Paste(ColorImage atlas, int icon, ReadOnlySpan<byte> cell)
    {
        var left = icon % Columns * CellSize;
        var top = icon / Columns * CellSize;

        if (left + CellSize > atlas.Width || top + CellSize > atlas.Height)
            throw new InvalidSpriteDataException($"Icon {icon} is outside the icon picture.");

        var pixels = (byte[])atlas.Bgra.Clone();
        var rowLength = CellSize * BytesPerPixel;

        for (var y = 0; y < CellSize; y++)
            cell.Slice(y * rowLength, rowLength).CopyTo(pixels.AsSpan(((top + y) * atlas.Width + left) * BytesPerPixel));

        return atlas with { Bgra = pixels };
    }

    public static byte[] Patch(byte[] archive, ReadOnlySpan<byte> cell)
    {
        var ads = FileIn(archive, AdsArchive);
        var atlas = FileIn(ads, Atlas);
        var icons = ColorHip.Encode(atlas, Paste(ColorHip.Decode(atlas), EIcon, cell));

        return LooseArchive.Merge(archive, [new ArchiveFile(AdsArchive, LooseArchive.Merge(ads, [new ArchiveFile(Atlas, icons)]))]);
    }

    private static byte[] FileIn(byte[] archive, string name) =>
        LooseArchive.Read(archive).FirstOrDefault(file => file.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Bytes
            ?? throw new InvalidSpriteDataException($"{name} is not in the archive.");
}
