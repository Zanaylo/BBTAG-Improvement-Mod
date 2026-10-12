using System.Text;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Online;

public static class SharedPaletteMessage
{
    public const int HeaderLength = 8;
    public const int VersionOffset = 4;
    public const int LongestFolder = 31;

    private const byte Version = 1;
    private const int ColorOffset = 5;
    private const int FolderLengthOffset = 6;
    private const int NameLengthOffset = 7;
    private const int LongestName = byte.MaxValue;

    private static readonly byte[] Magic = "BTIM"u8.ToArray();

    public static byte[] Encode(SharedPalette palette)
    {
        if (palette.Folder.Length > LongestFolder)
            throw new ArgumentException($"Character folders are at most {LongestFolder} characters long.", nameof(palette));

        var folder = Encoding.ASCII.GetBytes(palette.Folder);
        var name = Truncated(Encoding.UTF8.GetBytes(palette.Palette.Name));
        var impl = palette.Palette.ToBytes();

        byte[] header = [.. Magic, Version, (byte)palette.Color, (byte)folder.Length, (byte)name.Length];
        return [.. header, .. folder, .. name, .. impl];
    }

    public static SharedPalette? Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderLength || !bytes.StartsWith(Magic) || bytes[VersionOffset] != Version)
            return null;

        var folderLength = bytes[FolderLengthOffset];
        var nameLength = bytes[NameLengthOffset];
        var implOffset = HeaderLength + folderLength + nameLength;

        if (bytes.Length < implOffset)
            return null;

        var folder = Encoding.ASCII.GetString(bytes.Slice(HeaderLength, folderLength));

        if (!IsCharacterCode(folder))
            return null;

        var name = Encoding.UTF8.GetString(bytes.Slice(HeaderLength + folderLength, nameLength));

        try
        {
            return new SharedPalette(folder, bytes[ColorOffset], ImplPalette.Parse(name, bytes[implOffset..]));
        }
        catch (InvalidPaletteException)
        {
            return null;
        }
    }

    private static bool IsCharacterCode(string folder) =>
        folder.Length is > 0 and <= LongestFolder && folder.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    private static byte[] Truncated(byte[] name) => name.Length <= LongestName ? name : name[..LongestName];
}
