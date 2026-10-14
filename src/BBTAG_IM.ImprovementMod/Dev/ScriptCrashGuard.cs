using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod.Dev;

internal static unsafe class ScriptCrashGuard
{
    private const string User32Dll = "USER32.dll";
    private const string FatalTitle = "Error";
    private const string WarningTitle = "Continue";
    private const uint JapaneseCodePage = 932;
    private const int MostLetters = 2048;
    private const int Ok = 1;

    private static ILog s_log = null!;
    private static Func<string, bool> s_recover = null!;
    private static int s_quitsToSkip;

    private static delegate* unmanaged[Stdcall]<nint, byte*, byte*, uint, int> s_messageBox;
    private static delegate* unmanaged[Stdcall]<int, void> s_postQuitMessage;

    public static void Install(LoadedModule game, Func<string, bool> recover, ILog log)
    {
        s_log = log;
        s_recover = recover;

        s_messageBox = (delegate* unmanaged[Stdcall]<nint, byte*, byte*, uint, int>)ImportHook.Replace(
            game, User32Dll, "MessageBoxA", (nint)(delegate* unmanaged[Stdcall]<nint, byte*, byte*, uint, int>)&MessageBox);
        s_postQuitMessage = (delegate* unmanaged[Stdcall]<int, void>)ImportHook.Replace(
            game, User32Dll, "PostQuitMessage", (nint)(delegate* unmanaged[Stdcall]<int, void>)&PostQuitMessage);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int MessageBox(nint window, byte* text, byte* title, uint type)
    {
        var caption = Decode(title);

        if (caption is not (FatalTitle or WarningTitle))
            return s_messageBox(window, text, title, type);

        var message = Decode(text);
        SafeLog.Error(s_log, $"The game raised an error box \"{caption}\": {message}");

        if (!Recovered(message))
            return s_messageBox(window, text, title, type);

        if (caption == FatalTitle)
            Interlocked.Increment(ref s_quitsToSkip);

        SafeLog.Info(s_log, "A script edit caused it, so the last working script is back and the game keeps running.");
        return Ok;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void PostQuitMessage(int exitCode)
    {
        if (Interlocked.Exchange(ref s_quitsToSkip, 0) > 0)
            return;

        s_postQuitMessage(exitCode);
    }

    private static bool Recovered(string message)
    {
        try
        {
            return s_recover(message);
        }
        catch (Exception exception)
        {
            SafeLog.Error(s_log, $"The script could not be put back: {exception.Message}");
            return false;
        }
    }

    private static string Decode(byte* text)
    {
        if (text == null)
            return string.Empty;

        var length = 0;

        while (length < MostLetters && text[length] != 0)
            length++;

        var letters = stackalloc char[length];
        var written = Kernel32.MultiByteToWideChar(JapaneseCodePage, 0, text, length, letters, length);
        return written > 0 ? new string(letters, 0, written) : Marshal.PtrToStringAnsi((nint)text, length);
    }
}
