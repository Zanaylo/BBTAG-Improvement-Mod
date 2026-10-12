namespace BBTAG_IM.Core.Settings;

public sealed class BoxStyle
{
    public const float DefaultFill = 0.22f;
    public const float DefaultOutline = 0.9f;

    public bool Shown { get; set; } = true;

    public float Fill { get; set; } = DefaultFill;

    public float Outline { get; set; } = DefaultOutline;
}
