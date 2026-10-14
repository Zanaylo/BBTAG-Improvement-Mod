using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Scripting.Dev;

public sealed class ScriptFileFinder(IFileSystem files, DevFolder dev, IReadOnlyList<string> dataFolders)
{
    private const string CharacterFolder = "char";
    private const string ArchiveExtension = ".pac";
    private const string BinaryExtension = ".bin";

    public byte[]? Find(string code, bool effects)
    {
        var devBinary = dev.Script(code, effects).Binary;

        if (files.FileExists(devBinary))
            return files.ReadAllBytes(devBinary);

        var entryName = DevFolder.ScriptFileName(code, effects) + BinaryExtension;

        foreach (var dataFolder in dataFolders)
        {
            var archive = Path.Combine(dataFolder, CharacterFolder, DevFolder.ScriptArchiveName(code) + ArchiveExtension);

            if (files.FileExists(archive) && FromArchive(files.ReadAllBytes(archive), entryName) is { } bytes)
                return bytes;
        }

        return null;
    }

    private static byte[]? FromArchive(byte[] archive, string entryName)
    {
        var dataOffset = FpacArchive.DataOffset(archive);
        var entry = FpacArchive.Entries(archive.AsSpan(0, dataOffset))
            .FirstOrDefault(candidate => candidate.Name.Equals(entryName, StringComparison.OrdinalIgnoreCase));

        return entry is null ? null : archive.AsSpan((int)entry.Offset, entry.Size).ToArray();
    }
}
