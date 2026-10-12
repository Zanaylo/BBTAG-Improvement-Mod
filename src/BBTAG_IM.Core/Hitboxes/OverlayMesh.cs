using System.Numerics;
using System.Runtime.InteropServices;

namespace BBTAG_IM.Core.Hitboxes;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct OverlayVertex(float X, float Y, float Z, float Rhw, uint Color);

public sealed class OverlayMesh
{
    public const float BorderThickness = 2.5f;
    public const float OriginArm = 8f;
    public const float OriginThickness = 2f;

    private const float PixelCenter = 0.5f;
    private const float Depth = 0;
    private const float ReciprocalW = 1;
    private const int VerticesPerTriangle = 3;

    private readonly List<OverlayVertex> _vertices = [];

    public ReadOnlySpan<OverlayVertex> Vertices => CollectionsMarshal.AsSpan(_vertices);

    public int TriangleCount => _vertices.Count / VerticesPerTriangle;

    public void Clear() => _vertices.Clear();

    public void AddBox(Quad screen, uint color, float fillAlpha, float outlineAlpha)
    {
        if (fillAlpha > 0)
            AddQuad(screen, HitboxColors.WithAlpha(color, fillAlpha));

        if (outlineAlpha <= 0)
            return;

        var border = HitboxColors.WithAlpha(color, outlineAlpha);
        AddEdge(screen.A, screen.B, border, BorderThickness);
        AddEdge(screen.B, screen.C, border, BorderThickness);
        AddEdge(screen.C, screen.D, border, BorderThickness);
        AddEdge(screen.D, screen.A, border, BorderThickness);
    }

    public void AddOrigin(Vector2 point, uint color)
    {
        AddEdge(point - new Vector2(OriginArm, 0), point + new Vector2(OriginArm, 0), color, OriginThickness);
        AddEdge(point - new Vector2(0, OriginArm), point + new Vector2(0, OriginArm), color, OriginThickness);
    }

    private void AddEdge(Vector2 from, Vector2 to, uint color, float thickness)
    {
        if (from == to)
            return;

        var direction = Vector2.Normalize(to - from);
        var side = new Vector2(-direction.Y, direction.X) * (thickness / 2);

        AddQuad(new Quad(from + side, to + side, to - side, from - side), color);
    }

    private void AddQuad(Quad quad, uint color)
    {
        AddVertex(quad.A, color);
        AddVertex(quad.B, color);
        AddVertex(quad.C, color);
        AddVertex(quad.A, color);
        AddVertex(quad.C, color);
        AddVertex(quad.D, color);
    }

    private void AddVertex(Vector2 point, uint color) =>
        _vertices.Add(new OverlayVertex(point.X - PixelCenter, point.Y - PixelCenter, Depth, ReciprocalW, color));
}
