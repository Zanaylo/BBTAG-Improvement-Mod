using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Hantei.Games;

public sealed record ArchivedFile(string Archive, long Offset, int Length);

public sealed class ArchivedFiles(IFileSystem files, IReadOnlyDictionary<string, ArchivedFile> entries, Func<byte[], byte[]> decode) : IHanteiGameFiles
{
    public IEnumerable<string> Names => entries.Keys;

    public bool Contains(string name) => entries.ContainsKey(HanteiFileName.Normalize(name));

    public byte[] Read(string name)
    {
        if (!entries.TryGetValue(HanteiFileName.Normalize(name), out var entry))
            throw new InvalidHanteiDataException($"{name} is not in the game's archives.");

        return decode(files.ReadBytes(entry.Archive, entry.Offset, entry.Length));
    }
}
