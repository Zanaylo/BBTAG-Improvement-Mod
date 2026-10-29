using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod.Harness;

internal static unsafe class QuietWindow
{
    private const string User32Dll = "USER32.dll";
    private const uint NoActivateStyle = 0x08000000;
    private const uint VisibleStyle = 0x10000000;
    private const int HideCommand = 0;
    private const int ShowWithoutActivating = 4;
    private const nint Bottom = 1;
    private const uint NoSize = 0x0001;
    private const uint NoMove = 0x0002;
    private const uint NoActivate = 0x0010;

    private static nint s_window;

    private static delegate* unmanaged[Stdcall]<uint, char*, char*, uint, int, int, int, int, nint, nint, nint, nint, nint> s_createWindow;
    private static delegate* unmanaged[Stdcall]<nint, int, int> s_showWindow;
    private static delegate* unmanaged[Stdcall]<nint, nint, int, int, int, int, uint, int> s_setWindowPosition;
    private static delegate* unmanaged[Stdcall]<nint> s_getFocus;

    public static void Install(LoadedModule game)
    {
        s_createWindow = (delegate* unmanaged[Stdcall]<uint, char*, char*, uint, int, int, int, int, nint, nint, nint, nint, nint>)ImportHook.Replace(
            game, User32Dll, "CreateWindowExW", (nint)(delegate* unmanaged[Stdcall]<uint, char*, char*, uint, int, int, int, int, nint, nint, nint, nint, nint>)&CreateWindow);
        s_showWindow = (delegate* unmanaged[Stdcall]<nint, int, int>)ImportHook.Replace(
            game, User32Dll, "ShowWindow", (nint)(delegate* unmanaged[Stdcall]<nint, int, int>)&ShowWindow);
        s_setWindowPosition = (delegate* unmanaged[Stdcall]<nint, nint, int, int, int, int, uint, int>)ImportHook.Replace(
            game, User32Dll, "SetWindowPos", (nint)(delegate* unmanaged[Stdcall]<nint, nint, int, int, int, int, uint, int>)&SetWindowPosition);
        s_getFocus = (delegate* unmanaged[Stdcall]<nint>)ImportHook.Replace(
            game, User32Dll, "GetFocus", (nint)(delegate* unmanaged[Stdcall]<nint>)&GetFocus);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint CreateWindow(uint extendedStyle, char* className, char* title, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter)
    {
        var window = s_createWindow(extendedStyle | NoActivateStyle, className, title, style & ~VisibleStyle, x, y, width, height, parent, menu, instance, parameter);

        if (parent == 0 && window != 0 && s_window == 0)
            s_window = window;

        return window;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int ShowWindow(nint window, int command)
    {
        if (window != s_window || command == HideCommand)
            return s_showWindow(window, command);

        var result = s_showWindow(window, ShowWithoutActivating);
        s_setWindowPosition(window, Bottom, 0, 0, 0, 0, NoMove | NoSize | NoActivate);
        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int SetWindowPosition(nint window, nint after, int x, int y, int width, int height, uint flags) =>
        window == s_window
            ? s_setWindowPosition(window, Bottom, x, y, width, height, flags | NoActivate)
            : s_setWindowPosition(window, after, x, y, width, height, flags);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint GetFocus() => s_window != 0 ? s_window : s_getFocus();
}
