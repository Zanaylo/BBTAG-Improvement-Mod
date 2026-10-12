namespace BBTAG_IM.Core.Tests.Support;

internal static class PaletteMemory
{
    public const uint MirrorOffset = 0x800;
    public const int FileLength = 1024;

    private const int FileCount = 8;
    private const int LastIndex = 19;
    private const uint ListOffset = 0x100;
    private const uint DataOffset = 0x10000;
    private const uint DataStride = 0x1000;
    private const uint FileDataOffset = 0x1C;

    public static void Lay(FakeGameMemory memory, uint tableField, uint area, params int[] indexes)
    {
        memory.Map(tableField, sizeof(uint)).UInt32At(tableField, area);
        memory.Map(area, 8).UInt32At(area + 4, area + ListOffset);
        memory.Map(area + ListOffset, (LastIndex + 1) * FileCount * sizeof(uint));

        foreach (int index in indexes.Where(index => index is >= 0 and <= LastIndex))
        {
            for (int file = 0; file < FileCount; file++)
            {
                uint data = DataAddress(area, index, file);
                memory.UInt32At(area + ListOffset + (uint)(index * FileCount + file) * sizeof(uint), data);
                memory.Map(data, (int)(FileDataOffset + MirrorOffset) + FileLength);
            }
        }
    }

    public static void RemoveFile(FakeGameMemory memory, uint area, int index, int file) =>
        memory.UInt32At(area + ListOffset + (uint)(index * FileCount + file) * sizeof(uint), 0);

    public static uint FileAddress(uint area, int index, int file) => DataAddress(area, index, file) + FileDataOffset;

    private static uint DataAddress(uint area, int index, int file) => area + DataOffset + (uint)(index * FileCount + file) * DataStride;
}
