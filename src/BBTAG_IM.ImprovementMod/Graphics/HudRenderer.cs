using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Meter;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.ImprovementMod.Menu;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Graphics;

internal sealed unsafe class HudRenderer
{
    private const uint ColorVertexFormat = 0x004 | 0x040;
    private const uint TexturedVertexFormat = 0x004 | 0x040 | 0x100;
    private const int VerticesPerQuad = 6;
    private const int TrianglesPerQuad = 2;
    private const float PixelCenter = 0.5f;
    private const float Depth = 0;
    private const float ReciprocalW = 1;

    private readonly BackBufferPass _pass;
    private readonly MenuTextures _textures = new();
    private readonly HudScene _scene = new();
    private readonly Action<Direct3DDevice, DeviceViewport> _draw;

    private OverlayVertex[] _solid = [];
    private MenuVertex[] _glyphs = [];
    private nint _fontTexture;
    private GlyphSheet? _font;
    private Action<HudScene, float, float>? _compose;

    public HudRenderer(BackBufferPass pass)
    {
        _pass = pass;
        _draw = DrawScene;
        _textures.Released += () => _fontTexture = 0;
    }

    public void Draw(nint device, GlyphSheet font, Action<HudScene, float, float> compose)
    {
        _font = font;
        _compose = compose;
        _pass.Run(device, _draw);
    }

    public void Release() => _textures.ReleaseAll();

    private void DrawScene(Direct3DDevice device, DeviceViewport viewport)
    {
        if (_compose is null || _font is null)
            return;

        _scene.Clear();
        _compose(_scene, viewport.Width, viewport.Height);
        _textures.Use(device);
        DrawRects(device);
        DrawTexts(device, viewport, _font);
    }

    private void DrawRects(Direct3DDevice device)
    {
        var rects = _scene.Rects;

        if (rects.Count == 0)
            return;

        Ensure(ref _solid, rects.Count * VerticesPerQuad);

        for (var index = 0; index < rects.Count; index++)
        {
            var rect = rects[index];
            var left = rect.X - PixelCenter;
            var top = rect.Y - PixelCenter;
            var right = left + rect.Width;
            var bottom = top + rect.Height;
            var at = index * VerticesPerQuad;
            _solid[at] = Solid(left, top, rect.Color);
            _solid[at + 1] = Solid(right, top, rect.Color);
            _solid[at + 2] = Solid(left, bottom, rect.Color);
            _solid[at + 3] = Solid(right, top, rect.Color);
            _solid[at + 4] = Solid(right, bottom, rect.Color);
            _solid[at + 5] = Solid(left, bottom, rect.Color);
        }

        ScreenStates.PrepareColored(device, ColorVertexFormat);

        fixed (OverlayVertex* vertices = _solid)
            device.DrawTriangles(vertices, rects.Count * TrianglesPerQuad, Unsafe.SizeOf<OverlayVertex>());
    }

    private void DrawTexts(Direct3DDevice device, DeviceViewport viewport, GlyphSheet font)
    {
        if (_scene.Texts.Count == 0 || FontTexture(font) is not { } texture)
            return;

        var count = 0;

        foreach (var text in _scene.Texts)
        {
            foreach (var placed in font.Layout(text.Text, text.Scale))
            {
                Ensure(ref _glyphs, (count + 1) * VerticesPerQuad);
                AddGlyph(count++ * VerticesPerQuad, placed, text, font);
            }
        }

        if (count == 0)
            return;

        ScreenStates.PrepareTextured(device, TexturedVertexFormat);
        device.SetScissorRect(new WindowRect { Left = 0, Top = 0, Right = (int)viewport.Width, Bottom = (int)viewport.Height });
        device.SetTexture(0, texture);

        fixed (MenuVertex* vertices = _glyphs)
            device.DrawTriangles(vertices, count * TrianglesPerQuad, Unsafe.SizeOf<MenuVertex>());
    }

    private void AddGlyph(int at, PlacedGlyph placed, HudText text, GlyphSheet font)
    {
        var left = text.X + placed.X - PixelCenter;
        var top = text.Y + placed.Y - PixelCenter;
        var right = left + placed.Width;
        var bottom = top + placed.Height;
        var width = (float)font.Image.Width;
        var height = (float)font.Image.Height;
        var u0 = placed.Glyph.X / width;
        var v0 = placed.Glyph.Y / height;
        var u1 = (placed.Glyph.X + placed.Glyph.Width) / width;
        var v1 = (placed.Glyph.Y + placed.Glyph.Height) / height;
        _glyphs[at] = new MenuVertex(left, top, Depth, ReciprocalW, text.Color, u0, v0);
        _glyphs[at + 1] = new MenuVertex(right, top, Depth, ReciprocalW, text.Color, u1, v0);
        _glyphs[at + 2] = new MenuVertex(left, bottom, Depth, ReciprocalW, text.Color, u0, v1);
        _glyphs[at + 3] = new MenuVertex(right, top, Depth, ReciprocalW, text.Color, u1, v0);
        _glyphs[at + 4] = new MenuVertex(right, bottom, Depth, ReciprocalW, text.Color, u1, v1);
        _glyphs[at + 5] = new MenuVertex(left, bottom, Depth, ReciprocalW, text.Color, u0, v1);
    }

    private nint? FontTexture(GlyphSheet font)
    {
        if (_fontTexture == 0)
            _fontTexture = _textures.Create(font.Image.Width, font.Image.Height, MemoryMarshal.Cast<byte, uint>(font.Image.Bgra));

        return _fontTexture == 0 ? null : _fontTexture;
    }

    private static OverlayVertex Solid(float x, float y, uint color) => new(x, y, Depth, ReciprocalW, color);

    private static void Ensure<T>(ref T[] buffer, int length)
    {
        if (buffer.Length < length)
            Array.Resize(ref buffer, Math.Max(length, buffer.Length * 2));
    }
}
