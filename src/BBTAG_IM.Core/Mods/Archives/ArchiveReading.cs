using SharpCompress.Archives;
using SharpCompress.Common;

namespace BBTAG_IM.Core.Mods.Archives;

internal enum ArchiveReading
{
    EachEntry,
    InOrder,
    CompressedTar,
}

internal static class ArchiveReadings
{
    public static ArchiveReading Of(IArchive archive)
    {
        if (archive.Type == ArchiveType.GZip)
            return ArchiveReading.CompressedTar;

        return archive.IsSolid || archive.Type == ArchiveType.SevenZip ? ArchiveReading.InOrder : ArchiveReading.EachEntry;
    }

    public static bool IsFile(IEntry entry) => !entry.IsDirectory && string.IsNullOrEmpty(entry.LinkTarget) && entry.Key is not null;
}
