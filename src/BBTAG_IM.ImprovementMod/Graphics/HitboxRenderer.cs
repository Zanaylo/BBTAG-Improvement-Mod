using System.Runtime.CompilerServices;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.ImprovementMod.Graphics;

internal sealed unsafe class HitboxRenderer
{
    private const uint ScreenColorVertexFormat = 0x004 | 0x040;

    private readonly OverlayMesh _mesh = new();

    public void Draw(nint device, HitboxFrame frame, CameraView camera, ImprovementSettings settings) =>
        BackBufferPass.Run(device, (screen, viewport) => DrawFrame(screen, viewport, frame, camera, settings));

    private void DrawFrame(Direct3DDevice device, DeviceViewport deviceViewport, HitboxFrame frame, CameraView camera, ImprovementSettings settings)
    {
        var viewport = new Viewport(deviceViewport.X, deviceViewport.Y, deviceViewport.Width, deviceViewport.Height);
        _mesh.Clear();

        foreach (var shape in frame.Boxes)
        {
            var style = settings.StyleOf(shape.Kind);

            if (style.Shown)
                _mesh.AddBox(shape.World.Map(point => ScreenProjection.ToScreen(point, camera, viewport)), HitboxColors.Of(shape.Kind), style.Fill, style.Outline);
        }

        if (settings.HitboxOrigins)
        {
            foreach (var origin in frame.Origins)
                _mesh.AddOrigin(ScreenProjection.ToScreen(origin, camera, viewport), HitboxColors.Origin);
        }

        if (_mesh.TriangleCount == 0)
            return;

        ScreenStates.PrepareColored(device, ScreenColorVertexFormat);

        fixed (OverlayVertex* vertices = _mesh.Vertices)
            device.DrawTriangles(vertices, _mesh.TriangleCount, Unsafe.SizeOf<OverlayVertex>());
    }
}
