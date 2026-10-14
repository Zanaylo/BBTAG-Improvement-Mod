using System.Numerics;
using BBTAG_IM.Core.Hitboxes;

namespace BBTAG_IM.Core.Tests.Hitboxes;

public class HitboxGeometryTests
{
    private const int Precision = 3;

    private static readonly CollisionBox Box = new(BoxKind.Hurtbox, 100, 200, 50, 100);

    [Fact]
    public void Origin_EntityPosition_ConvertsGameUnitsIntoWorldUnits()
    {
        // Arrange
        Entity entity = CreateEntity(positionX: 100000, positionY: 50000);

        // Act
        Vector2 origin = HitboxGeometry.Origin(entity, BoxPlacement.Native);

        // Assert
        Assert.Equal(new Vector2(36, 18), origin);
    }

    [Fact]
    public void Origin_PivotAndOffsets_MovesThePositionByThem()
    {
        // Arrange
        Entity entity = CreateEntity(positionX: 100000, positionY: 50000, pivotX: 10000, offsetX: 5000, offsetY: 10000);

        // Act
        Vector2 origin = HitboxGeometry.Origin(entity, BoxPlacement.Native);

        // Assert
        Assert.Equal(new Vector2(34, 22), origin);
    }

    [Fact]
    public void Corners_FacingLeft_PlacesTheBoxRightOfAndBelowTheOriginInWorldUnits()
    {
        // Arrange
        Entity entity = CreateEntity();

        // Act
        Quad corners = HitboxGeometry.Corners(entity, Box, BoxPlacement.Native);

        // Assert
        Assert.Equal(new Quad(new(36, -73), new(54, -73), new(54, -109), new(36, -109)), corners);
    }

    [Fact]
    public void Corners_FacingRight_MirrorsTheBox()
    {
        // Arrange
        Entity entity = CreateEntity(facingLeft: false);

        // Act
        Quad corners = HitboxGeometry.Corners(entity, Box, BoxPlacement.Native);

        // Assert
        Assert.Equal(new Quad(new(-36, -73), new(-54, -73), new(-54, -109), new(-36, -109)), corners);
    }

    [Fact]
    public void Corners_HalfWidthScale_ShrinksTheBoxSideways()
    {
        // Arrange
        Entity entity = CreateEntity(scaleX: 500);

        // Act
        Quad corners = HitboxGeometry.Corners(entity, Box, BoxPlacement.Native);

        // Assert
        Assert.Equal(new Quad(new(18, -73), new(27, -73), new(27, -109), new(18, -109)), corners);
    }

    [Fact]
    public void Corners_RotatedAQuarterTurn_TurnsTheBoxAroundTheOrigin()
    {
        // Arrange
        Entity entity = CreateEntity(rotation: 90000);

        // Act
        Quad corners = HitboxGeometry.Corners(entity, Box, BoxPlacement.Native);

        // Assert
        Assert.Equal(73, corners.A.X, Precision);
        Assert.Equal(36, corners.A.Y, Precision);
    }

    [Fact]
    public void Corners_FacingRightAndRotated_TurnsTheOtherWay()
    {
        // Arrange
        Entity entity = CreateEntity(facingLeft: false, rotation: 90000);

        // Act
        Quad corners = HitboxGeometry.Corners(entity, Box, BoxPlacement.Native);

        // Assert
        Assert.Equal(-73, corners.A.X, Precision);
        Assert.Equal(36, corners.A.Y, Precision);
    }

    [Fact]
    public void Origin_Lift_RaisesTheOriginByThatManyWorldUnits()
    {
        // Arrange
        Entity entity = CreateEntity(positionX: 100000, positionY: 50000);

        // Act
        Vector2 origin = HitboxGeometry.Origin(entity, new BoxPlacement(1, 12));

        // Assert
        Assert.Equal(new Vector2(36, 30), origin);
    }

    [Fact]
    public void Corners_Lift_RaisesTheWholeBox()
    {
        // Arrange
        Entity entity = CreateEntity();

        // Act
        Quad corners = HitboxGeometry.Corners(entity, Box, new BoxPlacement(1, 12));

        // Assert
        Assert.Equal(new Quad(new(36, -61), new(54, -61), new(54, -97), new(36, -97)), corners);
    }

    [Fact]
    public void Corners_FateDrawnLarger_GrowsTheBoxAwayFromTheOrigin()
    {
        // Arrange
        Entity entity = CreateEntity();

        // Act
        Quad corners = HitboxGeometry.Corners(entity, Box, new BoxPlacement(1.05f, 0));

        // Assert
        Assert.Equal(new Quad(new(38, -77), new(57, -77), new(57, -115), new(38, -115)), corners);
    }

    private static Entity CreateEntity(
        bool facingLeft = true,
        int positionX = 0,
        int positionY = 0,
        int pivotX = 0,
        int offsetX = 0,
        int offsetY = 0,
        int rotation = 0,
        int scaleX = 1000) =>
        new(Entity.ActiveStatus, 0, 0, facingLeft, positionX, positionY, pivotX, offsetX, offsetY, rotation, scaleX, 1000, [Box]);
}
