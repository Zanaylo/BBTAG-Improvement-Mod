using System.Runtime.CompilerServices;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.ImprovementMod.Graphics;

internal sealed unsafe class HitboxRenderer
{
    private const uint ScreenColorVertexFormat = 0x004 | 0x040;

    private readonly OverlayMesh _mesh = new();
    private readonly BackBufferPass _pass;
    private readonly Action<Direct3DDevice, DeviceViewport> _drawFrame;

    private HitboxFrame _frame = HitboxFrame.Empty;
    private CameraView? _camera;
    private ImprovementSettings _settings = new();

    public HitboxRenderer(BackBufferPass pass)
    {
        _pass = pass;
        _drawFrame = DrawFrame;
    }

    public void Draw(nint device, HitboxFrame frame, CameraView camera, ImprovementSettings settings)
    {
        _frame = frame;
        _camera = camera;
        _settings = settings;
        _pass.Run(device, _drawFrame);
    }

    private void DrawFrame(Direct3DDevice device, DeviceViewport deviceViewport)
    {
        if (_camera is not { } camera)
            return;

        var viewport = new Viewport(deviceViewport.X, deviceViewport.Y, deviceViewport.Width, deviceViewport.Height);
        _mesh.Clear();
        AddBoxes(camera, viewport);
        AddOrigins(camera, viewport);

        if (_mesh.TriangleCount == 0)
            return;

        ScreenStates.PrepareColored(device, ScreenColorVertexFormat);

        fixed (OverlayVertex* vertices = _mesh.Vertices)
            device.DrawTriangles(vertices, _mesh.TriangleCount, Unsafe.SizeOf<OverlayVertex>());
    }

    private void AddBoxes(CameraView camera, Viewport viewport)
    {
        foreach (var shape in _frame.Boxes)
        {
            var style = _settings.StyleOf(shape.Kind);

            if (style.Shown)
                _mesh.AddBox(ScreenProjection.ToScreen(shape.World, camera, viewport), HitboxColors.Of(shape.Kind), style.Fill, style.Outline);
        }
    }

    private void AddOrigins(CameraView camera, Viewport viewport)
    {
        if (!_settings.HitboxOrigins)
            return;

        foreach (var origin in _frame.Origins)
            _mesh.AddOrigin(ScreenProjection.ToScreen(origin, camera, viewport), HitboxColors.Origin);
    }
}
