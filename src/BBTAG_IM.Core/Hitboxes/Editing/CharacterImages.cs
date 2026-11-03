using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed record CharacterImage(PlacedSprite Sprite, bool IsEffect, float PixelsPerUnit);

public sealed class CharacterImages(IFileSystem files, IReadOnlyList<string> dataFolders)
{
    private const string ImageExtension = ".hip";
    private const string CharacterImagesKind = "img";
    private const string EffectImagesKind = "vri";

    private readonly Dictionary<string, ImageArchive?> _archives = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CharacterImage?> _images = new(StringComparer.OrdinalIgnoreCase);

    public CharacterImage? Load(string code, SpriteFolder folder, string imageName)
    {
        var key = $"{code}/{folder}/{imageName}";

        if (_images.TryGetValue(key, out var known))
            return known;

        var fileName = Path.ChangeExtension(imageName, ImageExtension);
        var image = Find(code, folder, CharacterImagesKind, fileName) ?? Find(code, folder, EffectImagesKind, fileName);
        _images[key] = image;
        return image;
    }

    private CharacterImage? Find(string code, SpriteFolder folder, string kind, string fileName)
    {
        foreach (var directory in SpriteDirectories.SearchOrder(folder))
        {
            if (Archive(directory, code, kind) is not { } archive || !archive.Entries.TryGetValue(fileName, out var entry))
                continue;

            if (Read(archive, entry) is { } sprite)
                return new CharacterImage(sprite, kind == EffectImagesKind, SpriteDirectories.DefaultPixelsPerUnit(directory));
        }

        return null;
    }

    private PlacedSprite? Read(ImageArchive archive, FpacEntry entry)
    {
        try
        {
            return SpriteTrim.Trim(HipImage.Place(files.ReadBytes(archive.Path, entry.Offset, entry.Size)));
        }
        catch (Exception exception) when (exception is InvalidSpriteDataException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private ImageArchive? Archive(string directory, string code, string kind)
    {
        var key = $"{directory}/{code}/{kind}";

        if (_archives.TryGetValue(key, out var known))
            return known;

        var fileName = $"char_{code}_{kind}.pac";
        var path = dataFolders.Select(folder => Path.Combine(folder, directory, fileName)).FirstOrDefault(files.FileExists);
        var archive = path is null ? null : Index(path);
        _archives[key] = archive;
        return archive;
    }

    private ImageArchive? Index(string path)
    {
        try
        {
            var dataOffset = FpacArchive.DataOffset(files.ReadBytes(path, 0, FpacArchive.PrefixLength));
            var entries = FpacArchive.Entries(files.ReadBytes(path, 0, dataOffset))
                .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            return new ImageArchive(path, entries);
        }
        catch (Exception exception) when (exception is InvalidSpriteDataException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed record ImageArchive(string Path, IReadOnlyDictionary<string, FpacEntry> Entries);
}
