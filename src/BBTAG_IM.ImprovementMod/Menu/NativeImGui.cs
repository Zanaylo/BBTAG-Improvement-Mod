using System.Numerics;
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

internal static unsafe partial class NativeImGui
{
    [LibraryImport("cimgui", EntryPoint = "ImFontAtlas_SetTexID")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void SetFontTexture(ImFontAtlas* atlas, ulong texture);
}
