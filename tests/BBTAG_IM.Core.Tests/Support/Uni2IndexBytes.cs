using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal static class Uni2IndexBytes
{
    private const int HeaderLength = 64;
    private const int ArchiveNameField = 12;
    private const int FolderLength = 128;
    private const int FolderNameField = 12;
    private const int FolderGap = 4;
    private const int EntryLength = 80;
    private const int EntryNameField = 12;
    private const int ClosingGapLength = 4;

    public static byte[] Build(string archive, params (string Folder, (string Name, int Offset, int Length)[] Files)[] folders)
    {
        var fileCount = folders.Sum(folder => folder.Files.Length);
        var bytes = new byte[HeaderLength + folders.Length * FolderLength + FolderGap + fileCount * EntryLength - ClosingGapLength];
        var span = bytes.AsSpan();

        BinaryPrimitives.WriteInt32LittleEndian(span, folders.Length);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], fileCount);
        Encoding.ASCII.GetBytes(archive).CopyTo(span[ArchiveNameField..]);

        var folderAt = HeaderLength;
        var entryAt = HeaderLength + folders.Length * FolderLength + FolderGap;

        foreach (var (folder, files) in folders)
        {
            BinaryPrimitives.WriteInt32LittleEndian(span[folderAt..], files.Length);
            Encoding.ASCII.GetBytes(folder).CopyTo(span[(folderAt + FolderNameField)..]);
            folderAt += FolderLength;

            foreach (var (name, offset, length) in files)
            {
                BinaryPrimitives.WriteInt32LittleEndian(span[entryAt..], length);
                BinaryPrimitives.WriteInt32LittleEndian(span[(entryAt + 4)..], length);
                BinaryPrimitives.WriteInt32LittleEndian(span[(entryAt + 8)..], offset);
                Encoding.ASCII.GetBytes(name).CopyTo(span[(entryAt + EntryNameField)..]);
                entryAt += EntryLength;
            }
        }

        return bytes;
    }
}
