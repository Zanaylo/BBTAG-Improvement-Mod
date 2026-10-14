using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Palettes;

public sealed class GameColorCount(IGameMemory memory, uint? limit)
{
    private const string LimitName = "Color limit";
    private const uint LimitOffset = 8;

    private static readonly BytePattern LimitCheck = BytePattern.Parse("E8 ?? ?? ?? ?? 83 ?? ?? ?? 72 ?? ?? 8B ?? E8");

    public static uint LocateLimit(CodeImage image) => image.FindSingle(LimitCheck, LimitName) + LimitOffset;

    public int Current
    {
        get
        {
            if (limit is not { } address || !memory.CanRead(address, sizeof(byte)))
                return PaletteTable.StockColors;

            Span<byte> value = stackalloc byte[sizeof(byte)];
            memory.Read(address, value);
            return Math.Max(PaletteTable.StockColors, (int)value[0]);
        }
    }
}
