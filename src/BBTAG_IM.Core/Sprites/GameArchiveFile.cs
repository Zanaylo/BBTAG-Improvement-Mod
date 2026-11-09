using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Sprites;

public sealed class GameArchiveFile(IFileSystem fileSystem, IReadOnlyList<string> dataFolders)
{
    private static readonly byte[] ArchiveSignature = "FPAC"u8.ToArray();

    public byte[]? Read(string archive, string fileName)
    {
        foreach (var folder in dataFolders)
        {
            var path = Path.Combine(folder, archive);

            if (!fileSystem.FileExists(path))
                continue;

            return FindIn(fileSystem.ReadAllBytes(path), fileName);
        }

        return null;
    }

    private static byte[]? FindIn(byte[] archive, string fileName)
    {
        if (!archive.AsSpan().StartsWith(ArchiveSignature))
            return null;

        foreach (var file in LooseArchive.Read(archive))
        {
            if (file.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                return file.Bytes;

            if (FindIn(file.Bytes, fileName) is { } nested)
                return nested;
        }

        return null;
    }
}
