using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace BBTAG_IM.Core.Mods.Archives;

public sealed class ArchiveUnpacker : IArchiveUnpacker
{
    public void Unpack(string archive, string folder)
    {
        try
        {
            using var opened = ArchiveFactory.Open(archive);

            switch (ArchiveReadings.Of(opened))
            {
                case ArchiveReading.CompressedTar:
                    UnpackCompressedTar(archive, folder);
                    break;
                case ArchiveReading.InOrder:
                    UnpackInOrder(opened, folder);
                    break;
                default:
                    UnpackEachEntry(opened, folder);
                    break;
            }
        }
        catch (Exception exception) when (exception is not (ModArchiveException or OutOfMemoryException))
        {
            throw new ModArchiveException(exception.Message, exception);
        }
    }

    private static void UnpackCompressedTar(string archive, string folder)
    {
        using var reader = ReaderFactory.Open(archive);
        UnpackAll(reader, folder);
    }

    private static void UnpackInOrder(IArchive archive, string folder)
    {
        using var reader = archive.ExtractAllEntries();
        UnpackAll(reader, folder);
    }

    private static void UnpackAll(IReader reader, string folder)
    {
        while (reader.MoveToNextEntry())
            Write(reader.Entry, folder, reader.OpenEntryStream);
    }

    private static void UnpackEachEntry(IArchive archive, string folder)
    {
        foreach (var entry in archive.Entries)
            Write(entry, folder, entry.OpenEntryStream);
    }

    private static void Write(IEntry entry, string folder, Func<Stream> open)
    {
        if (!ArchiveReadings.IsFile(entry))
            return;

        if (entry.IsEncrypted)
            throw new ModArchiveException("it needs a password.");

        var path = ArchiveEntryPath.Inside(folder, entry.Key!);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var source = open();
        using var target = File.Create(path);
        source.CopyTo(target);
    }
}
