using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod.Graphics;

internal static unsafe class Direct3DHooks
{
    private const string Direct3DDll = "d3d9.dll";
    private const string CreateFactoryExport = "Direct3DCreate9Ex";
    private const int CreateDeviceSlot = 16;
    private const int CreateDeviceExSlot = 20;
    private const int ResetSlot = 16;
    private const int PresentSlot = 17;
    private const int PresentExSlot = 121;
    private const int ResetExSlot = 132;
    private const int PresentationIntervalOffset = 52;
    private const uint PresentIntervalImmediate = 0x80000000;

    private static FrameDispatcher s_frames = null!;
    private static bool s_turnVSyncOff;

    private static delegate* unmanaged[Stdcall]<uint, nint*, int> s_createFactory;
    private static delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint, byte*, nint*, int> s_createDevice;
    private static delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint, byte*, nint, nint*, int> s_createDeviceEx;
    private static delegate* unmanaged[Stdcall]<nint, byte*, int> s_reset;
    private static delegate* unmanaged[Stdcall]<nint, byte*, nint, int> s_resetEx;
    private static delegate* unmanaged[Stdcall]<nint, nint, nint, nint, nint, int> s_present;
    private static delegate* unmanaged[Stdcall]<nint, nint, nint, nint, nint, uint, int> s_presentEx;

    public static void Install(LoadedModule game, bool turnVSyncOff, FrameDispatcher frames)
    {
        s_frames = frames;
        s_turnVSyncOff = turnVSyncOff;
        s_createFactory = (delegate* unmanaged[Stdcall]<uint, nint*, int>)ImportHook.Replace(
            game, Direct3DDll, CreateFactoryExport, (nint)(delegate* unmanaged[Stdcall]<uint, nint*, int>)&CreateFactory);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int CreateFactory(uint sdkVersion, nint* factory)
    {
        var result = s_createFactory(sdkVersion, factory);

        if (result >= 0 && *factory != 0)
            HookFactory(*factory);

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int CreateDevice(nint factory, uint adapter, int deviceType, nint window, uint behavior, byte* parameters, nint* device)
    {
        TurnVSyncOff(parameters);
        var result = s_createDevice(factory, adapter, deviceType, window, behavior, parameters, device);

        if (result >= 0 && *device != 0)
            HookDevice(*device);

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int CreateDeviceEx(nint factory, uint adapter, int deviceType, nint window, uint behavior, byte* parameters, nint displayMode, nint* device)
    {
        TurnVSyncOff(parameters);
        var result = s_createDeviceEx(factory, adapter, deviceType, window, behavior, parameters, displayMode, device);

        if (result < 0 || *device == 0)
            return result;

        HookDevice(*device);
        HookExtendedDevice(*device);
        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Reset(nint device, byte* parameters)
    {
        TurnVSyncOff(parameters);
        s_frames.OnReset();
        return s_reset(device, parameters);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int ResetEx(nint device, byte* parameters, nint displayMode)
    {
        TurnVSyncOff(parameters);
        s_frames.OnReset();
        return s_resetEx(device, parameters, displayMode);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Present(nint device, nint sourceRect, nint destinationRect, nint window, nint dirtyRegion)
    {
        s_frames.OnPresent(device);
        return s_present(device, sourceRect, destinationRect, window, dirtyRegion);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int PresentEx(nint device, nint sourceRect, nint destinationRect, nint window, nint dirtyRegion, uint flags)
    {
        s_frames.OnPresent(device);
        return s_presentEx(device, sourceRect, destinationRect, window, dirtyRegion, flags);
    }

    private static void HookFactory(nint factory)
    {
        if (s_createDevice is not null)
            return;

        s_createDevice = (delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint, byte*, nint*, int>)VirtualTable.Replace(
            factory, CreateDeviceSlot, (nint)(delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint, byte*, nint*, int>)&CreateDevice);
        s_createDeviceEx = (delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint, byte*, nint, nint*, int>)VirtualTable.Replace(
            factory, CreateDeviceExSlot, (nint)(delegate* unmanaged[Stdcall]<nint, uint, int, nint, uint, byte*, nint, nint*, int>)&CreateDeviceEx);
    }

    private static void HookDevice(nint device)
    {
        if (s_present is not null)
            return;

        s_reset = (delegate* unmanaged[Stdcall]<nint, byte*, int>)VirtualTable.Replace(
            device, ResetSlot, (nint)(delegate* unmanaged[Stdcall]<nint, byte*, int>)&Reset);
        s_present = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint, nint, int>)VirtualTable.Replace(
            device, PresentSlot, (nint)(delegate* unmanaged[Stdcall]<nint, nint, nint, nint, nint, int>)&Present);
    }

    private static void HookExtendedDevice(nint device)
    {
        if (s_presentEx is not null)
            return;

        s_resetEx = (delegate* unmanaged[Stdcall]<nint, byte*, nint, int>)VirtualTable.Replace(
            device, ResetExSlot, (nint)(delegate* unmanaged[Stdcall]<nint, byte*, nint, int>)&ResetEx);
        s_presentEx = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint, nint, uint, int>)VirtualTable.Replace(
            device, PresentExSlot, (nint)(delegate* unmanaged[Stdcall]<nint, nint, nint, nint, nint, uint, int>)&PresentEx);
    }

    private static void TurnVSyncOff(byte* parameters)
    {
        if (s_turnVSyncOff && parameters is not null)
            *(uint*)(parameters + PresentationIntervalOffset) = PresentIntervalImmediate;
    }
}
