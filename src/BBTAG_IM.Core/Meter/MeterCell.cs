namespace BBTAG_IM.Core.Meter;

public readonly record struct MeterCell(FrameKind Kind, Invulnerability Invulnerability, CellMarks Marks)
{
    public static readonly MeterCell Empty = new(FrameKind.Empty, Invulnerability.None, CellMarks.None);
}
