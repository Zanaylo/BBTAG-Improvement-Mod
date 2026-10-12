using System.Runtime.InteropServices;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Graphics;

[StructLayout(LayoutKind.Sequential)]
internal struct DeviceViewport
{
    public uint X;
    public uint Y;
    public uint Width;
    public uint Height;
    public float MinimumZ;
    public float MaximumZ;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DeviceCreation
{
    public uint Adapter;
    public int DeviceType;
    public nint FocusWindow;
    public uint BehaviorFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LockedRect
{
    public int Pitch;
    public nint Bits;
}

internal readonly unsafe struct Direct3DDevice(nint device)
{
    private const int ReleaseSlot = 2;
    private const int StateBlockApplySlot = 5;
    private const int TextureLockSlot = 19;
    private const int TextureUnlockSlot = 20;
    private const int GetCreationParametersSlot = 9;
    private const int CreateTextureSlot = 23;
    private const int SetSamplerStateSlot = 69;
    private const int SetScissorRectSlot = 75;
    private const int DrawIndexedPrimitiveUserPointerSlot = 84;
    private const int Index16Format = 101;
    private const int DefaultPool = 0;
    private const int GetBackBufferSlot = 18;
    private const int SetRenderTargetSlot = 37;
    private const int GetRenderTargetSlot = 38;
    private const int BeginSceneSlot = 41;
    private const int EndSceneSlot = 42;
    private const int GetViewportSlot = 48;
    private const int SetRenderStateSlot = 57;
    private const int CreateStateBlockSlot = 59;
    private const int SetTextureSlot = 65;
    private const int SetTextureStageStateSlot = 67;
    private const int DrawPrimitiveUserPointerSlot = 83;
    private const int SetFvfSlot = 89;
    private const int SetVertexShaderSlot = 92;
    private const int SetPixelShaderSlot = 107;

    public static void Release(nint comObject)
    {
        if (comObject != 0)
            ((delegate* unmanaged[Stdcall]<nint, uint>)Slot(comObject, ReleaseSlot))(comObject);
    }

    public static void ApplyStateBlock(nint stateBlock) =>
        ((delegate* unmanaged[Stdcall]<nint, int>)Slot(stateBlock, StateBlockApplySlot))(stateBlock);

    public nint CreateStateBlock(int type)
    {
        nint block = 0;
        var result = ((delegate* unmanaged[Stdcall]<nint, int, nint*, int>)Slot(device, CreateStateBlockSlot))(device, type, &block);
        return result < 0 ? 0 : block;
    }

    public nint BackBuffer()
    {
        nint surface = 0;
        var result = ((delegate* unmanaged[Stdcall]<nint, uint, uint, int, nint*, int>)Slot(device, GetBackBufferSlot))(device, 0, 0, 0, &surface);
        return result < 0 ? 0 : surface;
    }

    public nint RenderTarget()
    {
        nint surface = 0;
        var result = ((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Slot(device, GetRenderTargetSlot))(device, 0, &surface);
        return result < 0 ? 0 : surface;
    }

    public void SetRenderTarget(nint surface) =>
        ((delegate* unmanaged[Stdcall]<nint, uint, nint, int>)Slot(device, SetRenderTargetSlot))(device, 0, surface);

    public DeviceViewport Viewport()
    {
        DeviceViewport viewport = default;
        ((delegate* unmanaged[Stdcall]<nint, DeviceViewport*, int>)Slot(device, GetViewportSlot))(device, &viewport);
        return viewport;
    }

    public bool BeginScene() => ((delegate* unmanaged[Stdcall]<nint, int>)Slot(device, BeginSceneSlot))(device) >= 0;

    public void EndScene() => ((delegate* unmanaged[Stdcall]<nint, int>)Slot(device, EndSceneSlot))(device);

    public void SetRenderState(int state, uint value) =>
        ((delegate* unmanaged[Stdcall]<nint, int, uint, int>)Slot(device, SetRenderStateSlot))(device, state, value);

    public void SetTextureStageState(uint stage, int type, uint value) =>
        ((delegate* unmanaged[Stdcall]<nint, uint, int, uint, int>)Slot(device, SetTextureStageStateSlot))(device, stage, type, value);

    public void ClearShaders()
    {
        ((delegate* unmanaged[Stdcall]<nint, nint, int>)Slot(device, SetVertexShaderSlot))(device, 0);
        ((delegate* unmanaged[Stdcall]<nint, nint, int>)Slot(device, SetPixelShaderSlot))(device, 0);
    }

    public void SetVertexFormat(uint format) =>
        ((delegate* unmanaged[Stdcall]<nint, uint, int>)Slot(device, SetFvfSlot))(device, format);

    public void DrawTriangles(void* vertices, int triangleCount, int stride) =>
        ((delegate* unmanaged[Stdcall]<nint, int, uint, void*, uint, int>)Slot(device, DrawPrimitiveUserPointerSlot))(
            device, PrimitiveType.TriangleList, (uint)triangleCount, vertices, (uint)stride);

    public static LockedRect LockTexture(nint texture)
    {
        LockedRect locked = default;
        var result = ((delegate* unmanaged[Stdcall]<nint, uint, LockedRect*, nint, uint, int>)Slot(texture, TextureLockSlot))(texture, 0, &locked, 0, 0);
        return result < 0 ? default : locked;
    }

    public static void UnlockTexture(nint texture) =>
        ((delegate* unmanaged[Stdcall]<nint, uint, int>)Slot(texture, TextureUnlockSlot))(texture, 0);

    public nint FocusWindow()
    {
        DeviceCreation creation = default;
        var result = ((delegate* unmanaged[Stdcall]<nint, DeviceCreation*, int>)Slot(device, GetCreationParametersSlot))(device, &creation);
        return result < 0 ? 0 : creation.FocusWindow;
    }

    public nint CreateTexture(uint width, uint height, uint usage, int format)
    {
        nint texture = 0;
        var result = ((delegate* unmanaged[Stdcall]<nint, uint, uint, uint, uint, int, int, nint*, nint, int>)Slot(device, CreateTextureSlot))(
            device, width, height, 1, usage, format, DefaultPool, &texture, 0);
        return result < 0 ? 0 : texture;
    }

    public void SetTexture(uint stage, nint texture) =>
        ((delegate* unmanaged[Stdcall]<nint, uint, nint, int>)Slot(device, SetTextureSlot))(device, stage, texture);

    public void SetSamplerState(uint sampler, int type, uint value) =>
        ((delegate* unmanaged[Stdcall]<nint, uint, int, uint, int>)Slot(device, SetSamplerStateSlot))(device, sampler, type, value);

    public void SetScissorRect(WindowRect rectangle) =>
        ((delegate* unmanaged[Stdcall]<nint, WindowRect*, int>)Slot(device, SetScissorRectSlot))(device, &rectangle);

    public void DrawIndexedTriangles(void* vertices, int vertexCount, ushort* indices, int triangleCount, int stride) =>
        ((delegate* unmanaged[Stdcall]<nint, int, uint, uint, uint, void*, int, void*, uint, int>)Slot(device, DrawIndexedPrimitiveUserPointerSlot))(
            device, PrimitiveType.TriangleList, 0, (uint)vertexCount, (uint)triangleCount, indices, Index16Format, vertices, (uint)stride);

    private static nint Slot(nint comObject, int index) => (*(nint**)comObject)[index];

    private static class PrimitiveType
    {
        public const int TriangleList = 4;
    }
}
