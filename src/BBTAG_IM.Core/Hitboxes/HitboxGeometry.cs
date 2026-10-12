using System.Numerics;

namespace BBTAG_IM.Core.Hitboxes;

public readonly record struct Quad(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
{
    public Quad Map(Func<Vector2, Vector2> transform) => new(transform(A), transform(B), transform(C), transform(D));
}

public static class HitboxGeometry
{
    private const float WorldUnitsPerGameUnit = 0.368f;
    private const float FixedPointScale = 1000f;
    private const float FullTurnDegrees = 360f;

    public static Vector2 Origin(Entity entity) =>
        new(
            ToWorld(entity.PositionX - entity.PivotX + entity.OffsetX),
            ToWorld(entity.PositionY + entity.OffsetY));

    public static Quad Corners(Entity entity, CollisionBox box)
    {
        var origin = Origin(entity);
        var scaleX = entity.ScaleX / FixedPointScale;
        var scaleY = entity.ScaleY / FixedPointScale;
        var direction = entity.FacingLeft ? 1 : -1;
        var left = direction * Scaled(box.X, scaleX);
        var width = direction * Scaled(box.Width, scaleX);
        var top = -Scaled(box.Y, scaleY);
        var height = -Scaled(box.Height, scaleY);
        var corners = new Quad(
            origin + new Vector2(left, top),
            origin + new Vector2(left + width, top),
            origin + new Vector2(left + width, top + height),
            origin + new Vector2(left, top + height));

        return corners.Map(point => Rotate(point, origin, RotationRadians(entity)));
    }

    private static float RotationRadians(Entity entity)
    {
        var degrees = entity.Rotation / FixedPointScale;

        if (!entity.FacingLeft && degrees != 0)
            degrees = FullTurnDegrees - degrees;

        return float.DegreesToRadians(degrees);
    }

    private static Vector2 Rotate(Vector2 point, Vector2 center, float radians)
    {
        if (radians == 0)
            return point;

        var (sin, cos) = MathF.SinCos(radians);
        var offset = point - center;

        return center + new Vector2(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos);
    }

    private static float ToWorld(int gameUnits) => MathF.Floor(gameUnits / FixedPointScale * WorldUnitsPerGameUnit);

    private static float Scaled(float length, float scale) => MathF.Floor(length * WorldUnitsPerGameUnit * scale);
}
