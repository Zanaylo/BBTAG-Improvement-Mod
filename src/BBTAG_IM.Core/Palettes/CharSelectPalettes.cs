using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Palettes;

public sealed class CharSelectPalettes(
    IFileSystem fileSystem,
    IReadOnlyList<string> dataFolders,
    PaletteCatalog catalog,
    Func<ImprovementSettings> settings,
    string cacheFolder)
{
    private const string EtcFolder = "etc";
    private const string ArchiveFolder = "char_select_pal";

    private readonly Dictionary<string, PatchedArchive> _patched = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _patchedLock = new();

    public string? Substitute(string? requestedPath)
    {
        if (CharSelectArchive.CodeOf(requestedPath) is not { } code || Characters.FolderOfImageCode(code) is not { } folder)
            return null;

        var palettes = Assigned(folder);

        if (palettes.Count == 0)
            return null;

        var fileName = Path.GetFileName(requestedPath!);

        if (Source(fileName) is not { } source)
            return null;

        lock (_patchedLock)
            return Patched(fileName, new PatchedArchive(source, palettes));
    }

    private string? Patched(string fileName, PatchedArchive wanted)
    {
        var path = Path.Combine(cacheFolder, fileName);

        if (_patched.TryGetValue(fileName, out var written) && written.SameAs(wanted) && fileSystem.FileExists(path))
            return path;

        try
        {
            fileSystem.WriteAllBytes(path, CharSelectArchive.Patch(fileSystem.ReadAllBytes(wanted.Source), wanted.Palettes));
            _patched[fileName] = wanted;
            return path;
        }
        catch (InvalidSpriteDataException)
        {
            return null;
        }
    }

    private Dictionary<int, ImplPalette> Assigned(string folder)
    {
        var assigned = new Dictionary<int, ImplPalette>();

        if (!settings().Palettes.TryGetValue(folder, out var colors))
            return assigned;

        var available = catalog.Load(folder);

        foreach (var (color, name) in colors)
        {
            if (!RandomPalette.IsChoice(name) && available.Find(name) is { } palette)
                assigned[color - 1] = palette;
        }

        return assigned;
    }

    private string? Source(string fileName) =>
        dataFolders.Select(folder => Path.Combine(folder, EtcFolder, ArchiveFolder, fileName)).FirstOrDefault(fileSystem.FileExists);

    private sealed record PatchedArchive(string Source, IReadOnlyDictionary<int, ImplPalette> Palettes)
    {
        public bool SameAs(PatchedArchive other) =>
            Source.Equals(other.Source, StringComparison.OrdinalIgnoreCase)
            && Palettes.Count == other.Palettes.Count
            && Palettes.All(entry => other.Palettes.TryGetValue(entry.Key, out var palette) && ReferenceEquals(palette, entry.Value));
    }
}
