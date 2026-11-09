namespace BBTAG_IM.Core.Meter;

public sealed record MeterLook(float Scale, float Opacity, bool Counts, bool Invulnerability, bool Partners, bool Totals, float OffsetX, float OffsetY);

public sealed record SideView(
    string PointName,
    string PartnerName,
    IReadOnlyList<MeterCell> Point,
    IReadOnlyList<MeterCell> Partner,
    MoveTiming? PointMove,
    MoveTiming? PartnerMove,
    int? Advantage,
    int? Gap,
    ComboReadout Combo);

public sealed record MeterView(SideView Player1, SideView Player2, int Freeze);
