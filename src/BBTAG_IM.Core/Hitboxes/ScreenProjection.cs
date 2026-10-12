using System.Numerics;
using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Hitboxes;

public sealed record Viewport(float X, float Y, float Width, float Height);

public static class ScreenProjection
{
    private const float Half = 0.5f;

    public static Vector2 ToScreen(Vector2 world, CameraView camera, Viewport viewport) =>
        Project(world, camera.View * camera.Projection, viewport);

    public static Quad ToScreen(Quad world, CameraView camera, Viewport viewport)
    {
        var viewProjection = camera.View * camera.Projection;

        return new Quad(
            Project(world.A, viewProjection, viewport),
            Project(world.B, viewProjection, viewport),
            Project(world.C, viewProjection, viewport),
            Project(world.D, viewProjection, viewport));
    }

    private static Vector2 Project(Vector2 world, Matrix4x4 viewProjection, Viewport viewport)
    {
        var clip = Vector4.Transform(new Vector4(world, 0, 1), viewProjection);
        var deviceX = clip.X / clip.W;
        var deviceY = clip.Y / clip.W;

        return new Vector2(
            MathF.Floor(viewport.X + (1 + deviceX) * viewport.Width * Half),
            MathF.Floor(viewport.Y + (1 - deviceY) * viewport.Height * Half));
    }
}
