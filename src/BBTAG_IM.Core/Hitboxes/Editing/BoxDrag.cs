using System.Numerics;
using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public enum BoxHandle
{
    None,
    Move,
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

public readonly record struct ScreenRect(float Left, float Top, float Right, float Bottom)
{
    public static ScreenRect Around(Quad quad) =>
        new(
            MathF.Min(MathF.Min(quad.A.X, quad.B.X), MathF.Min(quad.C.X, quad.D.X)),
            MathF.Min(MathF.Min(quad.A.Y, quad.B.Y), MathF.Min(quad.C.Y, quad.D.Y)),
            MathF.Max(MathF.Max(quad.A.X, quad.B.X), MathF.Max(quad.C.X, quad.D.X)),
            MathF.Max(MathF.Max(quad.A.Y, quad.B.Y), MathF.Max(quad.C.Y, quad.D.Y)));
}

public readonly record struct BoxAxes(float PixelsPerUnitX, float PixelsPerUnitY)
{
    private const int GameUnitsPerBoxUnit = 1000;
    private const int MeasuredUnits = 100;

    public static BoxAxes From(CameraView camera, Viewport viewport, GamePoint origin, bool facingLeft)
    {
        var backwards = facingLeft ? 1 : -1;
        var span = GameUnitsPerBoxUnit * MeasuredUnits;
        var start = Screen(origin, camera, viewport);
        var across = Screen(origin with { X = origin.X + backwards * span }, camera, viewport);
        var down = Screen(origin with { Y = origin.Y - span }, camera, viewport);

        return new BoxAxes((across.X - start.X) / MeasuredUnits, (down.Y - start.Y) / MeasuredUnits);
    }

    public Vector2 ToBoxUnits(Vector2 pixels) =>
        new(PixelsPerUnitX == 0 ? 0 : MathF.Round(pixels.X / PixelsPerUnitX), PixelsPerUnitY == 0 ? 0 : MathF.Round(pixels.Y / PixelsPerUnitY));

    private static Vector2 Screen(GamePoint point, CameraView camera, Viewport viewport) =>
        ScreenProjection.ToScreen(HitboxGeometry.World(point), camera, viewport);
}

public static class BoxHandles
{
    public const float GrabDistance = 6f;

    public static BoxHandle At(ScreenRect rectangle, Vector2 mouse)
    {
        if (mouse.X < rectangle.Left - GrabDistance || mouse.X > rectangle.Right + GrabDistance
            || mouse.Y < rectangle.Top - GrabDistance || mouse.Y > rectangle.Bottom + GrabDistance)
            return BoxHandle.None;

        var left = MathF.Abs(mouse.X - rectangle.Left) <= GrabDistance;
        var right = !left && MathF.Abs(mouse.X - rectangle.Right) <= GrabDistance;
        var top = MathF.Abs(mouse.Y - rectangle.Top) <= GrabDistance;
        var bottom = !top && MathF.Abs(mouse.Y - rectangle.Bottom) <= GrabDistance;

        return (left, right, top, bottom) switch
        {
            (true, _, true, _) => BoxHandle.TopLeft,
            (_, true, true, _) => BoxHandle.TopRight,
            (true, _, _, true) => BoxHandle.BottomLeft,
            (_, true, _, true) => BoxHandle.BottomRight,
            (true, _, _, _) => BoxHandle.Left,
            (_, true, _, _) => BoxHandle.Right,
            (_, _, true, _) => BoxHandle.Top,
            (_, _, _, true) => BoxHandle.Bottom,
            _ => BoxHandle.Move,
        };
    }
}

public static class BoxDrag
{
    public const float SmallestSide = 1;

    public static JonbinBox Apply(JonbinBox start, BoxHandle handle, Vector2 pixels, BoxAxes axes)
    {
        var delta = axes.ToBoxUnits(pixels);

        if (handle == BoxHandle.Move)
            return start with { X = start.X + delta.X, Y = start.Y + delta.Y };

        var screenLeft = handle is BoxHandle.Left or BoxHandle.TopLeft or BoxHandle.BottomLeft;
        var screenRight = handle is BoxHandle.Right or BoxHandle.TopRight or BoxHandle.BottomRight;
        var top = handle is BoxHandle.Top or BoxHandle.TopLeft or BoxHandle.TopRight;
        var bottom = handle is BoxHandle.Bottom or BoxHandle.BottomLeft or BoxHandle.BottomRight;
        var growsRight = axes.PixelsPerUnitX >= 0;
        var (near, far) = Resize(start.LowX, start.HighX, delta.X, growsRight ? screenLeft : screenRight, growsRight ? screenRight : screenLeft);
        var (upper, lower) = Resize(start.LowY, start.HighY, delta.Y, top, bottom);
        var (x, width) = Span(near, far, start.Width);
        var (y, height) = Span(upper, lower, start.Height);

        return start with { X = x, Y = y, Width = width, Height = height };
    }

    private static (float Start, float Length) Span(float low, float high, float startLength) =>
        startLength < 0 ? (high, low - high) : (low, high - low);

    private static (float Low, float High) Resize(float low, float high, float delta, bool moveLow, bool moveHigh)
    {
        if (moveLow)
            return (MathF.Min(low + delta, high - SmallestSide), high);

        if (moveHigh)
            return (low, MathF.Max(high + delta, low + SmallestSide));

        return (low, high);
    }
}
