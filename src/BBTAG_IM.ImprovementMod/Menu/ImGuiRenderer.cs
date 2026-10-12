using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Graphics;
using BBTAG_IM.Native;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct MenuVertex(float X, float Y, float Z, float Rhw, uint Color, float U, float V);

internal sealed unsafe class ImGuiRenderer(ILog log)
{
    private const uint MenuVertexFormat = 0x004 | 0x040 | 0x100;
    private const uint DynamicUsage = 0x200;
    private const int ArgbFormat = 21;
    private const int BytesPerPixel = 4;
    private const int IndicesPerTriangle = 3;
    private const float PixelCenter = 0.5f;

    private nint _fontTexture;
    private MenuVertex[] _vertices = [];
    private bool _reportedBadCommand;

    public void Release()
    {
        Direct3DDevice.Release(_fontTexture);
        _fontTexture = 0;
    }

    public bool PrepareFonts(Direct3DDevice device)
    {
        if (_fontTexture != 0)
            return true;

        var fonts = ImGui.GetIO().Fonts;
        fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height);
        var texture = device.CreateTexture((uint)width, (uint)height, DynamicUsage, ArgbFormat);

        if (texture == 0)
            return false;

        if (!CopyPixels(texture, pixels, width, height))
        {
            Direct3DDevice.Release(texture);
            return false;
        }

        _fontTexture = texture;
        NativeImGui.SetFontTexture(fonts.NativePtr, (ulong)texture);
        return true;
    }

    public void Render(Direct3DDevice device, ImDrawDataPtr drawData)
    {
        var data = drawData.NativePtr;

        if (data->CmdListsCount == 0)
            return;

        ScreenStates.PrepareTextured(device, MenuVertexFormat);

        for (var list = 0; list < data->CmdListsCount; list++)
            RenderList(device, ((ImDrawList**)data->CmdLists.Data)[list]);
    }

    private void RenderList(Direct3DDevice device, ImDrawList* list)
    {
        var vertexCount = list->VtxBuffer.Size;
        ConvertVertices((ImDrawVert*)list->VtxBuffer.Data, vertexCount);
        var indices = (ushort*)list->IdxBuffer.Data;
        var commands = (NativeDrawCommand*)list->CmdBuffer.Data;

        fixed (MenuVertex* vertices = _vertices)
        {
            for (var index = 0; index < list->CmdBuffer.Size; index++)
            {
                var command = commands[index];

                if (command.UserCallback != 0 || !CanDraw(command, list->IdxBuffer.Size))
                    continue;

                device.SetScissorRect(new WindowRect
                {
                    Left = (int)command.ClipRect.X,
                    Top = (int)command.ClipRect.Y,
                    Right = (int)command.ClipRect.Z,
                    Bottom = (int)command.ClipRect.W,
                });
                device.SetTexture(0, _fontTexture);
                device.DrawIndexedTriangles(
                    vertices, vertexCount, indices + command.IndexOffset, (int)command.ElementCount / IndicesPerTriangle, Unsafe.SizeOf<MenuVertex>());
            }
        }
    }

    private bool CanDraw(NativeDrawCommand command, int indexCount)
    {
        if (MenuDrawCommands.CanDraw(command.TextureId, (ulong)_fontTexture, command.IndexOffset, command.ElementCount, indexCount))
            return true;

        if (!_reportedBadCommand)
        {
            _reportedBadCommand = true;
            SafeLog.Error(log, $"The menu skipped a draw command it could not trust (texture 0x{command.TextureId:X}, indices {command.IndexOffset}+{command.ElementCount} of {indexCount}).");
        }

        return false;
    }

    private void ConvertVertices(ImDrawVert* source, int count)
    {
        if (_vertices.Length < count)
            _vertices = new MenuVertex[count];

        for (var index = 0; index < count; index++)
        {
            var vertex = source[index];
            _vertices[index] = new MenuVertex(
                vertex.pos.X - PixelCenter,
                vertex.pos.Y - PixelCenter,
                0,
                1,
                OverlayColors.SwapRedAndBlue(vertex.col),
                vertex.uv.X,
                vertex.uv.Y);
        }
    }

    private static bool CopyPixels(nint texture, byte* pixels, int width, int height)
    {
        var locked = Direct3DDevice.LockTexture(texture);

        if (locked.Bits == 0)
            return false;

        var rowLength = width * BytesPerPixel;

        for (var row = 0; row < height; row++)
        {
            var destination = new Span<byte>((void*)(locked.Bits + row * locked.Pitch), rowLength);
            new ReadOnlySpan<byte>(pixels + row * rowLength, rowLength).CopyTo(destination);
            OverlayColors.ConvertRgbaToBgra(destination);
        }

        Direct3DDevice.UnlockTexture(texture);
        return true;
    }
}
