using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod.Errors;

internal static unsafe class GameErrorGuard
{
    private const string User32Dll = "USER32.dll";
    private const uint JapaneseCodePage = 932;
    private const int MostLetters = 2048;
    private const int Ok = 1;

    private static ILog s_log = null!;
    private static MatchExit s_matchExit = null!;
    private static Func<string, bool> s_rollBackScript = _ => false;
    private static int s_quitsToSkip;

    private static delegate* unmanaged[Stdcall]<nint, byte*, byte*, uint, int> s_messageBox;
    private static delegate* unmanaged[Stdcall]<int, void> s_postQuitMessage;

    public static void Install(LoadedModule game, MatchExit matchExit, ILog log)
    {
        s_log = log;
        s_matchExit = matchExit;

        var managed = (nint)(delegate* unmanaged[Stdcall]<nint, byte*, byte*, uint, int>)&MessageBox;
        var replacement = FrameRecovery.AddMessageBoxThunk(managed) is { } thunk ? (nint)thunk : managed;
        s_messageBox = (delegate* unmanaged[Stdcall]<nint, byte*, byte*, uint, int>)ImportHook.Replace(game, User32Dll, "MessageBoxA", replacement);
        s_postQuitMessage = (delegate* unmanaged[Stdcall]<int, void>)ImportHook.Replace(
            game, User32Dll, "PostQuitMessage", (nint)(delegate* unmanaged[Stdcall]<int, void>)&PostQuitMessage);
    }

    public static void RollBackScriptsWith(Func<string, bool> rollBackScript) => s_rollBackScript = rollBackScript;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int MessageBox(nint window, byte* text, byte* title, uint type)
    {
        var caption = Decode(title);

        if (!ErrorBoxPolicy.IsGameError(caption))
            return s_messageBox(window, text, title, type);

        var message = Decode(text);
        SafeLog.Error(s_log, $"The game raised an error box \"{caption}\": {message}");

        switch (ErrorBoxPolicy.Decide(caption, () => RolledBack(message), LeftMatch))
        {
            case ErrorBoxAction.ScriptRolledBack:
                SkipQuitAfter(caption);
                SafeLog.Info(s_log, "A script edit caused it, so the last working script is back and the game keeps running.");
                return Ok;
            case ErrorBoxAction.Continue:
                SafeLog.Info(s_log, "It is only a warning, so the game keeps running without the box.");
                return Ok;
            case ErrorBoxAction.BackToCharacterSelect:
                return LeaveMatch(caption);
            default:
                SafeLog.Error(s_log, "No match was running, so the game closes after the box.");
                return s_messageBox(window, text, title, type);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void PostQuitMessage(int exitCode)
    {
        if (Interlocked.Exchange(ref s_quitsToSkip, 0) > 0)
            return;

        s_postQuitMessage(exitCode);
    }

    private static int LeaveMatch(string caption)
    {
        if (FrameRecovery.ResumeIfAllowed() is { } resume)
        {
            SafeLog.Error(s_log, "The game was not closed: that step was skipped and the match goes back to character select.");
            return (int)resume;
        }

        SkipQuitAfter(caption);
        SafeLog.Error(s_log, "The game was not closed: the match goes back to character select.");
        return Ok;
    }

    private static void SkipQuitAfter(string caption)
    {
        if (caption == ErrorBoxPolicy.FatalTitle)
            Interlocked.Increment(ref s_quitsToSkip);
    }

    private static bool LeftMatch()
    {
        try
        {
            return s_matchExit.ToCharacterSelect();
        }
        catch (Exception exception)
        {
            SafeLog.Error(s_log, $"The match could not be left: {exception.Message}");
            return false;
        }
    }

    private static bool RolledBack(string message)
    {
        try
        {
            return s_rollBackScript(message);
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
