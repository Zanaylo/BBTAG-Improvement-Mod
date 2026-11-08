using System.Buffers.Binary;
using System.Text;
using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Hantei.Games;

public static class Uni2Archives
{
    public const string ArchiveFolder = "d";
    public const string DataIndex = "hexeojmpimrjs";
    public const string EnglishIndex = "oMtaqooqotvvonnpwmq";
    public const string ScriptIndex = "kluioyxovozlort";
    public const string SystemIndex = "rcwWiiqjmxpmojs";

    private const int HeaderLength = 64;
    private const int FolderCountField = 0;
    private const int FileCountField = 4;
    private const int ArchiveNameField = 12;
    private const int ArchiveNameLength = 52;
    private const int FolderLength = 128;
    private const int FolderFileCountField = 0;
    private const int FolderNameField = 12;
    private const int FolderNameLength = 116;
    private const int EntryLength = 80;
    private const int EntrySizeField = 0;
    private const int EntryOffsetField = 8;
    private const int EntryNameField = 12;
    private const int EntryNameLength = 64;
    private const int TableGapLength = 4;

    public static IHanteiGameFiles Open(IFileSystem files, string root)
    {
        var folder = Path.Combine(root, ArchiveFolder);
        Dictionary<string, ArchivedFile> entries = new(StringComparer.OrdinalIgnoreCase);

        foreach (var index in new[] { DataIndex, EnglishIndex, ScriptIndex, SystemIndex }.Select(name => Path.Combine(folder, name)).Where(files.FileExists))
            ReadIndex(files.ReadAllBytes(index), folder, entries);

        return new ArchivedFiles(files, entries, bytes => bytes);
    }

    private static void ReadIndex(ReadOnlySpan<byte> index, string folder, Dictionary<string, ArchivedFile> entries)
    {
        if (index.Length < HeaderLength)
            throw new InvalidHanteiDataException("The UNDER NIGHT archive index is cut short.");

        var folderCount = BinaryPrimitives.ReadInt32LittleEndian(index[FolderCountField..]);
        var fileCount = BinaryPrimitives.ReadInt32LittleEndian(index[FileCountField..]);
        var tableStart = HeaderLength + folderCount * FolderLength + TableGapLength;

        if (folderCount < 0 || fileCount < 0 || tableStart + fileCount * EntryLength - TableGapLength > index.Length)
            throw new InvalidHanteiDataException("The UNDER NIGHT archive index is cut short.");

        var archive = Path.Combine(folder, Text(index.Slice(ArchiveNameField, ArchiveNameLength)));
        var entry = tableStart;

        for (var at = HeaderLength; at < HeaderLength + folderCount * FolderLength; at += FolderLength)
        {
            var folderName = Text(index.Slice(at + FolderNameField, FolderNameLength));
            var filesInFolder = BinaryPrimitives.ReadInt32LittleEndian(index[(at + FolderFileCountField)..]);

            for (var file = 0; file < filesInFolder; file++, entry += EntryLength)
                entries[HanteiFileName.Normalize($"{folderName}/{Text(index.Slice(entry + EntryNameField, EntryNameLength))}")] = ReadEntry(index[entry..], archive);
        }
    }

    private static ArchivedFile ReadEntry(ReadOnlySpan<byte> entry, string archive) => new(
        archive,
        BinaryPrimitives.ReadUInt32LittleEndian(entry[EntryOffsetField..]),
        BinaryPrimitives.ReadInt32LittleEndian(entry[EntrySizeField..]));

    private static string Text(ReadOnlySpan<byte> field)
    {
        var end = field.IndexOf((byte)0);
        return Encoding.Latin1.GetString(end < 0 ? field : field[..end]);
    }
}
