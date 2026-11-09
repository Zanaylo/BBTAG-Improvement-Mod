namespace BBTAG_IM.Core.Meter;

public sealed record ComboReadout(int Hits, int Damage, int Blocks, int Chip)
{
    public static readonly ComboReadout None = new(0, 0, 0, 0);
}
