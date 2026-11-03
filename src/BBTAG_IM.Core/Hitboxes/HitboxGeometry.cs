using System.Numerics;

namespace BBTAG_IM.Core.Hitboxes;

public readonly record struct Quad(Vector2 A, Vector2 B, Vector2 C, Vector2 D);

public static class HitboxGeometry
{
    private const float WorldUnitsPerGameUnit = 0.368f / 1000f;

    public static Vector2 World(GamePoint point) => new(ToWorld(point.X), ToWorld(point.Y));

    public static Quad World(GameRect rectangle) =>
        new(
            new Vector2(ToWorld(rectangle.Left), ToWorld(rectangle.Top)),
            new Vector2(ToWorld(rectangle.Right), ToWorld(rectangle.Top)),
            new Vector2(ToWorld(rectangle.Right), ToWorld(rectangle.Bottom)),
            new Vector2(ToWorld(rectangle.Left), ToWorld(rectangle.Bottom)));

    private static float ToWorld(int gameUnits) => gameUnits * WorldUnitsPerGameUnit;
}
