namespace BBTAG_IM.Core.Meter;

[Flags]
public enum CellMarks : byte
{
    None = 0,
    Projectile = 1,
    Armor = 2,
    HitStart = 4,
    Connected = 8,
}
