using System.Numerics;
using BBTAG_IM.Core.Hitboxes;

namespace BBTAG_IM.Core.Tests.Hitboxes;

public class OverlayMeshTests
{
    private const int FillVertices = 6;
    private const int EdgeVertices = 6;
    private const float PixelCenter = 0.5f;
    private const uint Green = 0xFF3CDC3C;
    private const float HalfVisible = 0.5f;
    private const float Opaque = 1f;
    private const float Invisible = 0f;

    private static readonly Quad Square = new(new(10, 10), new(20, 10), new(20, 20), new(10, 20));

    public OverlayMesh CreateOverlayMesh() => new();

    [Fact]
    public void AddBox_HalfVisibleFill_FillsTheQuadWithTheColorAtHalfAlpha()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();

        // Act
        mesh.AddBox(Square, Green, HalfVisible, Opaque);

        // Assert
        Assert.Equal(
            [Fill(10, 10), Fill(20, 10), Fill(20, 20), Fill(10, 10), Fill(20, 20), Fill(10, 20)],
            mesh.Vertices[..FillVertices].ToArray());
    }

    [Fact]
    public void AddBox_OpaqueOutline_DrawsTheBorderInTheFullColor()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();

        // Act
        mesh.AddBox(Square, Green, HalfVisible, Opaque);

        // Assert
        Assert.All(mesh.Vertices[FillVertices..].ToArray(), vertex => Assert.Equal(Green, vertex.Color));
    }

    [Fact]
    public void AddBox_Square_AddsTwoFillTrianglesAndTwoTrianglesPerEdge()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();

        // Act
        mesh.AddBox(Square, Green, HalfVisible, Opaque);

        // Assert
        Assert.Equal(10, mesh.TriangleCount);
    }

    [Fact]
    public void AddBox_InvisibleFill_DrawsOnlyTheBorder()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();

        // Act
        mesh.AddBox(Square, Green, Invisible, Opaque);

        // Assert
        Assert.Equal(4 * EdgeVertices, mesh.Vertices.Length);
    }

    [Fact]
    public void AddBox_InvisibleOutline_DrawsOnlyTheFill()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();

        // Act
        mesh.AddBox(Square, Green, HalfVisible, Invisible);

        // Assert
        Assert.Equal(FillVertices, mesh.Vertices.Length);
    }

    [Fact]
    public void AddBox_TopEdge_MakesTheBorderAsThickAsTheEdgeIsLong()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();
        float halfThickness = OverlayMesh.BorderThickness / 2;

        // Act
        mesh.AddBox(Square, Green, HalfVisible, Opaque);

        // Assert
        Assert.Equal(
            [new Vector2(10, 10 + halfThickness), new Vector2(20, 10 + halfThickness), new Vector2(20, 10 - halfThickness)],
            mesh.Vertices.Slice(FillVertices, 3).ToArray().Select(vertex => new Vector2(vertex.X + PixelCenter, vertex.Y + PixelCenter)));
    }

    [Fact]
    public void AddBox_EdgeOfZeroLength_SkipsThatEdge()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();
        Quad flat = Square with { B = Square.A };

        // Act
        mesh.AddBox(flat, Green, HalfVisible, Opaque);

        // Assert
        Assert.Equal(FillVertices + 3 * EdgeVertices, mesh.Vertices.Length);
    }

    [Fact]
    public void AddOrigin_Point_DrawsAHorizontalAndAVerticalBarCrossingThere()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();

        // Act
        mesh.AddOrigin(new Vector2(50, 50), Green);

        // Assert
        Vector2[] points = mesh.Vertices.ToArray().Select(vertex => new Vector2(vertex.X + PixelCenter, vertex.Y + PixelCenter)).ToArray();
        Assert.Equal(2 * EdgeVertices, points.Length);
        Assert.Equal((50 - OverlayMesh.OriginArm, 50 + OverlayMesh.OriginArm), (points.Min(point => point.X), points.Max(point => point.X)));
        Assert.Equal((50 - OverlayMesh.OriginArm, 50 + OverlayMesh.OriginArm), (points.Min(point => point.Y), points.Max(point => point.Y)));
    }

    [Fact]
    public void Clear_AfterBoxes_LeavesNothingToDraw()
    {
        // Arrange
        OverlayMesh mesh = CreateOverlayMesh();
        mesh.AddBox(Square, Green, HalfVisible, Opaque);

        // Act
        mesh.Clear();

        // Assert
        Assert.Equal(0, mesh.TriangleCount);
    }

    private static OverlayVertex Fill(float x, float y) => new(x - PixelCenter, y - PixelCenter, 0, 1, 0x7F3CDC3C);
}
