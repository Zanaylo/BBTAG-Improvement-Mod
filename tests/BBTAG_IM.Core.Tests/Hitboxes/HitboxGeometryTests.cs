using System.Numerics;
using BBTAG_IM.Core.Hitboxes;

namespace BBTAG_IM.Core.Tests.Hitboxes;

public class HitboxGeometryTests
{
    private const int Precision = 3;

    [Fact]
    public void World_GamePosition_ConvertsGameUnitsIntoWorldUnits()
    {
        // Arrange
        GamePoint position = new(100000, 50000);

        // Act
        Vector2 world = HitboxGeometry.World(position);

        // Assert
        Assert.Equal(36.8f, world.X, Precision);
        Assert.Equal(18.4f, world.Y, Precision);
    }

    [Fact]
    public void World_GameRectangle_StartsAtTheTopLeftAndGoesAround()
    {
        // Arrange
        GameRect rectangle = new(Left: -50000, Right: 100000, Bottom: 0, Top: 250000);

        // Act
        Quad world = HitboxGeometry.World(rectangle);

        // Assert
        Assert.Equal((-18.4f, 92f), (world.A.X, world.A.Y), new PointComparer(Precision));
        Assert.Equal((36.8f, 92f), (world.B.X, world.B.Y), new PointComparer(Precision));
        Assert.Equal((36.8f, 0f), (world.C.X, world.C.Y), new PointComparer(Precision));
        Assert.Equal((-18.4f, 0f), (world.D.X, world.D.Y), new PointComparer(Precision));
    }

    private sealed class PointComparer(int precision) : IEqualityComparer<(float X, float Y)>
    {
        public bool Equals((float X, float Y) expected, (float X, float Y) actual) =>
            MathF.Round(expected.X - actual.X, precision) == 0 && MathF.Round(expected.Y - actual.Y, precision) == 0;

        public int GetHashCode((float X, float Y) point) => 0;
    }
}
