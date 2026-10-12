using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

[StructLayout(LayoutKind.Explicit, Size = 56)]
internal struct NativeDrawCommand
{
    [FieldOffset(0)]
    public Vector4 ClipRect;

    [FieldOffset(16)]
    public ulong TextureId;

    [FieldOffset(24)]
    public uint VertexOffset;

    [FieldOffset(28)]
    public uint IndexOffset;

    [FieldOffset(32)]
    public uint ElementCount;

    [FieldOffset(36)]
    public nint UserCallback;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct NativePoint(float X, float Y);

internal static unsafe partial class NativeImGui
{
    private const string Library = "cimgui";
    private const uint OpaqueWhite = 0xFFFFFFFF;

    private static readonly NativePoint FirstCorner = new(0, 0);
    private static readonly NativePoint LastCorner = new(1, 1);

    public static bool BeginTabItem(string label, ImGuiTabItemFlags flags) => BeginTabItem(label, null, flags);

    public static void AddImage(ImDrawListPtr draw, nint texture, Vector2 min, Vector2 max) =>
        AddImage(draw.NativePtr, (ulong)texture, new NativePoint(min.X, min.Y), new NativePoint(max.X, max.Y), FirstCorner, LastCorner, OpaqueWhite);

    [LibraryImport(Library, EntryPoint = "ImFontAtlas_SetTexID")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SetFontTexture(ImFontAtlas* atlas, ulong texture);

    [LibraryImport(Library, EntryPoint = "ImDrawList_AddImage")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void AddImage(ImDrawList* draw, ulong texture, NativePoint min, NativePoint max, NativePoint firstCorner, NativePoint lastCorner, uint tint);

    [LibraryImport(Library, EntryPoint = "igBeginTabItem", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool BeginTabItem(string label, bool* open, ImGuiTabItemFlags flags);
}
