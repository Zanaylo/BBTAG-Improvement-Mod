using System.Numerics;
using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Hitboxes;

public sealed record Viewport(float X, float Y, float Width, float Height);

public static class ScreenProjection
{
    private const float Half = 0.5f;

    public static Vector2 ToScreen(Vector2 world, CameraView camera, Viewport viewport)
    {
        var clip = Vector4.Transform(new Vector4(world, 0, 1), camera.View * camera.Projection);
        var deviceX = clip.X / clip.W;
        var deviceY = clip.Y / clip.W;

        return new Vector2(
            MathF.Floor(viewport.X + (1 + deviceX) * viewport.Width * Half),
            MathF.Floor(viewport.Y + (1 - deviceY) * viewport.Height * Half));
    }
}
