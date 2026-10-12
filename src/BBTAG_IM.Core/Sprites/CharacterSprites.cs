using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Sprites;

public sealed class CharacterSprites(IFileSystem fileSystem, IReadOnlyList<string> dataFolders, ILog log)
{
    private const string CharacterFolder = "char";

    public IndexedSprite? Load(string paletteFolder)
    {
        if (Characters.ImageCodeOf(paletteFolder) is not { } code)
            return null;

        var fileName = $"char_{code}_img.pac";

        if (Find(fileName) is not { } path)
            return null;

        try
        {
            return Read(path);
        }
        catch (Exception exception) when (exception is InvalidSpriteDataException or IOException or UnauthorizedAccessException)
        {
            log.Info($"{paletteFolder} palettes show colors instead of the character: {fileName} could not be read. {exception.Message}");
            return null;
        }
    }

    private string? Find(string fileName) =>
        dataFolders.Select(folder => Path.Combine(folder, CharacterFolder, fileName)).FirstOrDefault(fileSystem.FileExists);

    private IndexedSprite Read(string path)
    {
        var dataOffset = FpacArchive.DataOffset(fileSystem.ReadBytes(path, 0, FpacArchive.PrefixLength));
        var entries = FpacArchive.Entries(fileSystem.ReadBytes(path, 0, dataOffset));

        if (IdleSprite.Pick(entries) is not { } idle)
            throw new InvalidSpriteDataException("It holds no standing sprite.");

        return SpriteCrop.MainBody(HipImage.Decode(fileSystem.ReadBytes(path, idle.Offset, idle.Size)));
    }
}
