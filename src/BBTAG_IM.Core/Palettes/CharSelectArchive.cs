using System.Text.RegularExpressions;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Palettes;

public static partial class CharSelectArchive
{
    private const int HeaderLength = 0x20;
    private const int CharacterFile = 0;

    private static readonly byte[] PaletteSignature = "HPAL"u8.ToArray();

    public static string? CodeOf(string? path)
    {
        if (path is null)
            return null;

        var match = ArchiveName().Match(Path.GetFileName(path));
        return match.Success ? match.Groups[1].Value : null;
    }

    public static byte[] Patch(ReadOnlySpan<byte> archive, IReadOnlyDictionary<int, ImplPalette> palettes)
    {
        var patched = archive.ToArray();

        foreach (var entry in FpacArchive.Entries(patched))
        {
            if (ColorOf(entry.Name) is not { } color || !palettes.TryGetValue(color, out var palette))
                continue;

            var colors = Colors(patched, entry);

            if (!colors.IsEmpty)
                palette.File(CharacterFile).CopyTo(colors);
        }

        return patched;
    }

    private static int? ColorOf(string entryName)
    {
        var match = EntryName().Match(entryName);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private static Span<byte> Colors(byte[] archive, FpacEntry entry)
    {
        if (entry.Size < HeaderLength + ImplPalette.FileLength || entry.Offset + entry.Size > archive.Length)
            return [];

        var bytes = archive.AsSpan((int)entry.Offset, entry.Size);
        return bytes.StartsWith(PaletteSignature) ? bytes.Slice(HeaderLength, ImplPalette.FileLength) : [];
    }

    [GeneratedRegex(@"^char_select_([a-z0-9]+)_pal\.pac$", RegexOptions.IgnoreCase)]
    private static partial Regex ArchiveName();

    [GeneratedRegex(@"(\d+)_00\.hpl$", RegexOptions.IgnoreCase)]
    private static partial Regex EntryName();
}
