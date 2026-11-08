using System.Text;
using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Hantei.Games;

public static class MbtlArchives
{
    public const string ExecutableName = "MBTL.exe";

    private const string PatternSignature = "Hantei6DataFile";
    private const char FolderSeparator = '/';

    private static readonly Dictionary<string, string> ArchiveOfFolder = new(StringComparer.Ordinal)
    {
        [HanteiFileName.DataFolder] = "data008.bin",
        ["___English"] = "data000.bin",
        ["script"] = "data011.bin",
        ["System"] = "data014.bin",
    };

    public static IHanteiGameFiles Open(IFileSystem files, string root)
    {
        foreach (var index in MbtlIndex.Candidates(files.ReadAllBytes(Path.Combine(root, ExecutableName))))
        {
            var game = new ArchivedFiles(files, EntriesOf(index, root), bytes => MbtlCipher.Decode(bytes, index.Key));

            if (ReadsBack(game))
                return game;
        }

        throw new InvalidHanteiDataException(MbtlIndex.Unsupported);
    }

    private static Dictionary<string, ArchivedFile> EntriesOf(MbtlIndex index, string root)
    {
        Dictionary<string, ArchivedFile> entries = new(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in index.Entries)
        {
            if (entry.Size > 0 && ArchiveOf(entry.Name) is { } archive)
                entries[entry.Name] = new ArchivedFile(Path.Combine(root, archive), entry.Offset, entry.Size);
        }

        return entries;
    }

    private static string? ArchiveOf(string name)
    {
        var folderEnd = name.IndexOf(FolderSeparator);
        return folderEnd < 0 ? null : ArchiveOfFolder.GetValueOrDefault(name[..folderEnd]);
    }

    private static bool ReadsBack(ArchivedFiles game)
    {
        try
        {
            return game.Contains(MbtlIndex.AnchorName) && Encoding.Latin1.GetString(game.Read(MbtlIndex.AnchorName)).StartsWith(PatternSignature, StringComparison.Ordinal);
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }
}
