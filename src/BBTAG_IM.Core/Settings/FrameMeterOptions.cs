using BBTAG_IM.Core.Meter;

namespace BBTAG_IM.Core.Settings;

public sealed class FrameMeterOptions
{
    public const float SmallestScale = 0.5f;
    public const float LargestScale = 2.5f;
    public const float FaintestOpacity = 0.1f;

    public bool Shown { get; set; }

    public float Scale { get; set; } = 1f;

    public float Opacity { get; set; } = 1f;

    public bool Counts { get; set; } = true;

    public bool Invulnerability { get; set; } = true;

    public bool Partners { get; set; } = true;

    public bool Totals { get; set; } = true;

    public float OffsetX { get; set; }

    public float OffsetY { get; set; }

    public AutoPauseOptions AutoPause { get; set; } = new();

    public MeterLook Look() =>
        new(Math.Clamp(Scale, SmallestScale, LargestScale), Math.Clamp(Opacity, FaintestOpacity, 1f), Counts, Invulnerability, Partners, Totals, OffsetX, OffsetY);
}

public sealed class AutoPauseOptions
{
    public const int MostStops = 8;
    public const int MostHits = 99;

    public bool Player1 { get; set; } = true;

    public bool Player2 { get; set; }

    public bool OnMoveStart { get; set; }

    public bool OnHit { get; set; }

    public bool OnCombo { get; set; }

    public List<int> ComboStops { get; set; } = [3];

    public bool OnBlock { get; set; }

    public List<int> BlockStops { get; set; } = [2];
}
