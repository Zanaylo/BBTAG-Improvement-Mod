namespace BBTAG_IM.Core.Meter;

public static class MeterColors
{
    public const uint Panel = 0xC8080E1C;
    public const uint PanelEdge = 0xFF2E5BD8;
    public const uint Lane = 0x70000000;
    public const uint Player1 = 0xFFE8405A;
    public const uint Player2 = 0xFF3F86FF;
    public const uint Name = 0xFFFFFFFF;
    public const uint Move = 0xFFFFE27A;
    public const uint Label = 0xFF8E9BB8;
    public const uint Value = 0xFFFFFFFF;
    public const uint Plus = 0xFF6FE38F;
    public const uint Minus = 0xFFFF6A6A;
    public const uint Count = 0xFF101010;
    public const uint Projectile = 0xFFFF9A2E;
    public const uint HitStart = 0xFFFFFFFF;
    public const uint Armor = 0xFFFFC832;

    private const uint AlphaMask = 0xFF000000;
    private const int AlphaShift = 24;

    public static uint Of(FrameKind kind) => kind switch
    {
        FrameKind.Idle => 0xFF4A566E,
        FrameKind.Movement => 0xFF3A92BE,
        FrameKind.Startup => 0xFF35C96C,
        FrameKind.Active => 0xFFFF3F3F,
        FrameKind.Recovery => 0xFF3A6BFF,
        FrameKind.Hitstun => 0xFFF5C628,
        FrameKind.Blockstun => 0xFFB8922A,
        FrameKind.Tech => 0xFFB25CFF,
        FrameKind.Leaving => 0xA05E5280,
        _ => 0,
    };

    public static uint OfInvulnerability(Invulnerability covered)
    {
        if (covered == Invulnerability.Full)
            return 0xFFFFFFFF;

        if (covered == Invulnerability.Throw)
            return 0xFFFF7FD4;

        if (covered == Invulnerability.Projectile)
            return 0xFFFFB050;

        return (covered & Invulnerability.Strike) == Invulnerability.Strike ? 0xFF7FE3FF : 0xFFA9B4C8;
    }

    public static uint Fade(uint color, float opacity)
    {
        var alpha = (uint)Math.Round((color >> AlphaShift) * Math.Clamp(opacity, 0, 1));
        return (color & ~AlphaMask) | (alpha << AlphaShift);
    }
}
