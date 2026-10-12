namespace BBTAG_IM.Core.Hitboxes;

public static class HitboxColors
{
    public const uint Hurtbox = 0xFF3CDC3C;
    public const uint Hitbox = 0xFFFF3C3C;
    public const uint Origin = 0xC8FFFF00;

    private const uint ColorMask = 0x00FFFFFF;
    private const int AlphaShift = 24;
    private const float FullAlpha = 255f;

    public static uint Of(BoxKind kind) => kind == BoxKind.Hitbox ? Hitbox : Hurtbox;

    public static uint WithAlpha(uint color, float alpha) =>
        (color & ColorMask) | ((uint)(Math.Clamp(alpha, 0, 1) * FullAlpha) << AlphaShift);
}
