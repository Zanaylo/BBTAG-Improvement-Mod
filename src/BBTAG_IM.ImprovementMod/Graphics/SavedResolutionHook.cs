using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Display;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod.Graphics;

internal static unsafe class SavedResolutionHook
{
    private const string Kernel32Dll = "KERNEL32.dll";

    private static SavedResolutionMap s_map = null!;

    private static delegate* unmanaged[Stdcall]<char*, char*, int, char*, uint> s_getProfileInt;
    private static delegate* unmanaged[Stdcall]<char*, char*, char*, char*, int> s_writeProfileString;

    public static void Install(LoadedModule game, SavedResolutionMap map)
    {
        s_map = map;
        s_getProfileInt = (delegate* unmanaged[Stdcall]<char*, char*, int, char*, uint>)ImportHook.Replace(
            game, Kernel32Dll, "GetPrivateProfileIntW", (nint)(delegate* unmanaged[Stdcall]<char*, char*, int, char*, uint>)&GetPrivateProfileIntW);
        s_writeProfileString = (delegate* unmanaged[Stdcall]<char*, char*, char*, char*, int>)ImportHook.Replace(
            game, Kernel32Dll, "WritePrivateProfileStringW", (nint)(delegate* unmanaged[Stdcall]<char*, char*, char*, char*, int>)&WritePrivateProfileStringW);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint GetPrivateProfileIntW(char* section, char* key, int defaultValue, char* file)
    {
        var value = s_getProfileInt(section, key, defaultValue, file);
        return IsResolution(key) ? (uint)s_map.ToRunning((int)value) : value;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int WritePrivateProfileStringW(char* section, char* key, char* value, char* file)
    {
        var text = Marshal.PtrToStringUni((nint)value);

        if (text is null || !IsResolution(key))
            return s_writeProfileString(section, key, value, file);

        fixed (char* saved = s_map.ToSavedText(text))
            return s_writeProfileString(section, key, saved, file);
    }

    private static bool IsResolution(char* key) => SavedResolutionMap.IsResolutionSetting(Marshal.PtrToStringUni((nint)key));
}
