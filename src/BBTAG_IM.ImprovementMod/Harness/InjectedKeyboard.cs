using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Harness;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod.Harness;

internal static unsafe class InjectedKeyboard
{
    private const string User32Dll = "USER32.dll";
    private const int KeyCount = 256;
    private const short Down = unchecked((short)0x8000);

    private static InjectedKeyQueue s_keys = null!;

    private static delegate* unmanaged[Stdcall]<byte*, int> s_getKeyboardState;
    private static delegate* unmanaged[Stdcall]<int, short> s_getAsyncKeyState;
    private static delegate* unmanaged[Stdcall]<int, short> s_getKeyState;

    public static void Install(LoadedModule game, InjectedKeyQueue keys)
    {
        s_keys = keys;
        s_getKeyboardState = (delegate* unmanaged[Stdcall]<byte*, int>)ImportHook.Replace(
            game, User32Dll, "GetKeyboardState", (nint)(delegate* unmanaged[Stdcall]<byte*, int>)&GetKeyboardState);
        s_getAsyncKeyState = (delegate* unmanaged[Stdcall]<int, short>)ImportHook.Replace(
            game, User32Dll, "GetAsyncKeyState", (nint)(delegate* unmanaged[Stdcall]<int, short>)&GetAsyncKeyState);
        s_getKeyState = (delegate* unmanaged[Stdcall]<int, short>)ImportHook.Replace(
            game, User32Dll, "GetKeyState", (nint)(delegate* unmanaged[Stdcall]<int, short>)&GetKeyState);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetKeyboardState(byte* state)
    {
        var result = s_getKeyboardState(state);

        if (state is null)
            return result;

        var keys = new Span<byte>(state, KeyCount);

        if (result == 0)
            keys.Clear();

        s_keys.Merge(keys);
        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static short GetAsyncKeyState(int key) => s_keys.IsDown(key) ? (short)(s_getAsyncKeyState(key) | Down) : s_getAsyncKeyState(key);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static short GetKeyState(int key) => s_keys.IsDown(key) ? (short)(s_getKeyState(key) | Down) : s_getKeyState(key);
}
