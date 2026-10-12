using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Palettes;

public sealed class PaletteTable(IGameMemory memory, uint tableField)
{
    public const int StockColors = 20;

    private const int CharacterFile = 0;
    private const uint ListOffset = 4;
    private const uint FileDataOffset = 0x1C;
    private const uint MirrorOffset = 0x800;
    private const int PointerSize = sizeof(uint);

    public static int PartnerOf(int color) => color > 0 ? color - 1 : color + 1;

    public uint? FileList => memory.ReadPointer(tableField) is { } table ? memory.ReadPointer(table + ListOffset) : null;

    public bool IsLoaded(int color) => HasFile(color, CharacterFile);

    public bool HasFile(int color, int file) => FileList is { } files && WritableFile(files, color, file) is not null;

    public PaletteColors Read(int color)
    {
        var bytes = new byte[PaletteColors.Length];

        if (FileList is not { } files)
            return PaletteColors.From(bytes);

        for (var file = 0; file < PaletteColors.FileCount; file++)
        {
            if (WritableFile(files, color, file) is { } address)
                memory.Read(address, bytes.AsSpan(file * PaletteColors.FileLength, PaletteColors.FileLength));
        }

        return PaletteColors.From(bytes);
    }

    public int Write(int color, PaletteColors colors)
    {
        if (FileList is not { } files)
            return 0;

        var written = 0;

        for (var file = 0; file < PaletteColors.FileCount; file++)
        {
            if (WritableFile(files, color, file) is not { } address)
                continue;

            memory.Write(address, colors.File(file));
            memory.Write(address + MirrorOffset, colors.File(file));
            written++;
        }

        return written;
    }

    private uint? WritableFile(uint files, int color, int file)
    {
        if (color < 0)
            return null;

        if (memory.ReadPointer(files + (uint)((color * PaletteColors.FileCount + file) * PointerSize)) is not { } start)
            return null;

        var address = start + FileDataOffset;
        var writable = memory.CanWrite(address, PaletteColors.FileLength) && memory.CanWrite(address + MirrorOffset, PaletteColors.FileLength);

        return writable ? address : null;
    }
}
