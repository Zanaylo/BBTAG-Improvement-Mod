using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.Payload.Hooks;

internal static unsafe class FileApiHooks
{
    private const string Kernel32Dll = "KERNEL32.dll";
    private const int MaximumLoggedRedirects = 40;
    private const int MaximumLoggedEnumerations = 20;
    private const int MaximumLoggedFailures = 5;
    private const int MaximumLoggedMissing = 200;
    private const nint InvalidHandle = -1;
    private const char Wildcard = '*';
    private const char SingleWildcard = '?';

    private static ModFileRedirector s_redirector = null!;
    private static ILog s_log = null!;
    private static int s_loggedRedirects;
    private static int s_loggedEnumerations;
    private static int s_loggedFailures;
    private static int s_loggedMissing;

    private static delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint> s_kernelCreateFileW;
    private static delegate* unmanaged[Stdcall]<byte*, uint, uint, nint, uint, uint, nint, nint> s_createFileA;
    private static delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint> s_createFileW;
    private static delegate* unmanaged[Stdcall]<char*, int, void*, int> s_getFileAttributesExW;
    private static delegate* unmanaged[Stdcall]<byte*, void*, nint> s_findFirstFileA;
    private static delegate* unmanaged[Stdcall]<byte*, int, void*, int, void*, uint, nint> s_findFirstFileExA;

    public static void Install(LoadedModule game, ModFileRedirector redirector, ILog log)
    {
        s_redirector = redirector;
        s_log = log;
        s_kernelCreateFileW = (delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint>)
            Kernel32.GetProcAddress(Kernel32.GetModuleHandle(Kernel32Dll), "CreateFileW");

        s_createFileA = (delegate* unmanaged[Stdcall]<byte*, uint, uint, nint, uint, uint, nint, nint>)ImportHook.Replace(
            game, Kernel32Dll, "CreateFileA", (nint)(delegate* unmanaged[Stdcall]<byte*, uint, uint, nint, uint, uint, nint, nint>)&CreateFileA);
        s_createFileW = (delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint>)ImportHook.Replace(
            game, Kernel32Dll, "CreateFileW", (nint)(delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint>)&CreateFileW);
        s_getFileAttributesExW = (delegate* unmanaged[Stdcall]<char*, int, void*, int>)ImportHook.Replace(
            game, Kernel32Dll, "GetFileAttributesExW", (nint)(delegate* unmanaged[Stdcall]<char*, int, void*, int>)&GetFileAttributesExW);
        s_findFirstFileA = (delegate* unmanaged[Stdcall]<byte*, void*, nint>)ImportHook.Replace(
            game, Kernel32Dll, "FindFirstFileA", (nint)(delegate* unmanaged[Stdcall]<byte*, void*, nint>)&FindFirstFileA);
        s_findFirstFileExA = (delegate* unmanaged[Stdcall]<byte*, int, void*, int, void*, uint, nint>)ImportHook.Replace(
            game, Kernel32Dll, "FindFirstFileExA", (nint)(delegate* unmanaged[Stdcall]<byte*, int, void*, int, void*, uint, nint>)&FindFirstFileExA);

        log.Info("File hooks installed: CreateFileA, CreateFileW, GetFileAttributesExW, FindFirstFileA, FindFirstFileExA.");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint CreateFileA(byte* fileName, uint access, uint share, nint security, uint disposition, uint flags, nint template)
    {
        var requested = Marshal.PtrToStringAnsi((nint)fileName);
        var redirected = RedirectOpen(requested, access, disposition);

        if (redirected is null)
            return CheckedOpen(requested, disposition, s_createFileA(fileName, access, share, security, disposition, flags, template));

        fixed (char* path = redirected)
            return CheckedOpen(requested, disposition, s_kernelCreateFileW(path, access, share, security, disposition, flags, template));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint CreateFileW(char* fileName, uint access, uint share, nint security, uint disposition, uint flags, nint template)
    {
        var requested = Marshal.PtrToStringUni((nint)fileName);
        var redirected = RedirectOpen(requested, access, disposition);

        if (redirected is null)
            return CheckedOpen(requested, disposition, s_createFileW(fileName, access, share, security, disposition, flags, template));

        fixed (char* path = redirected)
            return CheckedOpen(requested, disposition, s_createFileW(path, access, share, security, disposition, flags, template));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int GetFileAttributesExW(char* fileName, int infoLevel, void* information)
    {
        var requested = Marshal.PtrToStringUni((nint)fileName);
        var redirected = RedirectLookup(requested);

        if (redirected is null)
            return CheckedLookup(requested, s_getFileAttributesExW(fileName, infoLevel, information));

        fixed (char* path = redirected)
            return CheckedLookup(requested, s_getFileAttributesExW(path, infoLevel, information));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint FindFirstFileA(byte* fileName, void* findData)
    {
        var ansi = RedirectFindAnsi(fileName);

        if (ansi == 0)
            return CheckedFind(fileName, s_findFirstFileA(fileName, findData));

        try
        {
            return CheckedFind(fileName, s_findFirstFileA((byte*)ansi, findData));
        }
        finally
        {
            Marshal.FreeHGlobal(ansi);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint FindFirstFileExA(byte* fileName, int infoLevel, void* findData, int searchOp, void* searchFilter, uint flags)
    {
        var ansi = RedirectFindAnsi(fileName);

        if (ansi == 0)
            return CheckedFind(fileName, s_findFirstFileExA(fileName, infoLevel, findData, searchOp, searchFilter, flags));

        try
        {
            return CheckedFind(fileName, s_findFirstFileExA((byte*)ansi, infoLevel, findData, searchOp, searchFilter, flags));
        }
        finally
        {
            Marshal.FreeHGlobal(ansi);
        }
    }

    private static nint CheckedOpen(string? requested, uint disposition, nint handle)
    {
        if (handle == InvalidHandle && disposition == ModFileRedirector.OpenExisting)
            LogMissing(requested);

        return handle;
    }

    private static int CheckedLookup(string? requested, int succeeded)
    {
        if (succeeded == 0)
            LogMissing(requested);

        return succeeded;
    }

    private static nint CheckedFind(byte* fileName, nint handle)
    {
        if (handle != InvalidHandle)
            return handle;

        var requested = Marshal.PtrToStringAnsi((nint)fileName);

        if (requested?.IndexOfAny([Wildcard, SingleWildcard]) < 0)
            LogMissing(requested);

        return handle;
    }

    private static void LogMissing(string? requested)
    {
        var error = Marshal.GetLastSystemError();

        try
        {
            var key = s_redirector.GameFileKey(requested);

            if (key is not null && Interlocked.Increment(ref s_loggedMissing) <= MaximumLoggedMissing)
                s_log.Info($"Missing {key} (Windows error {error}).");
        }
        catch (IOException)
        {
        }
        finally
        {
            Marshal.SetLastSystemError(error);
        }
    }

    private static string? RedirectOpen(string? path, uint access, uint disposition)
    {
        try
        {
            return Logged(path, s_redirector.RedirectOpen(path, access, disposition)) ?? s_redirector.AnchorToGame(path);
        }
        catch (Exception exception)
        {
            LogFailure(path, exception);
            return null;
        }
    }

    private static string? RedirectLookup(string? path)
    {
        try
        {
            return Logged(path, s_redirector.RedirectLookup(path)) ?? s_redirector.AnchorToGame(path);
        }
        catch (Exception exception)
        {
            LogFailure(path, exception);
            return null;
        }
    }

    private static nint RedirectFindAnsi(byte* fileName)
    {
        var path = Marshal.PtrToStringAnsi((nint)fileName);

        if (path is not null && path.IndexOfAny([Wildcard, SingleWildcard]) >= 0)
        {
            LogEnumeration(path);
            return s_redirector.AnchorToGame(path) is { } anchored ? ToAnsiOrZero(anchored) : 0;
        }

        var redirected = RedirectLookup(path);
        return redirected is null ? 0 : ToAnsiOrZero(redirected);
    }

    private static nint ToAnsiOrZero(string path)
    {
        var ansi = Marshal.StringToHGlobalAnsi(path);

        if (Marshal.PtrToStringAnsi(ansi) == path)
            return ansi;

        Marshal.FreeHGlobal(ansi);
        LogFailure(path, new HookException("The mod path cannot be written in the system code page, so the game keeps its native file."));
        return 0;
    }

    private static string? Logged(string? requested, string? redirected)
    {
        if (redirected is not null && Interlocked.Increment(ref s_loggedRedirects) <= MaximumLoggedRedirects)
            s_log.Info($"Redirect {requested} -> {redirected}");

        return redirected;
    }

    private static void LogEnumeration(string pattern)
    {
        if (Interlocked.Increment(ref s_loggedEnumerations) <= MaximumLoggedEnumerations)
            s_log.Info($"The game listed {pattern}; listings are not merged with mod files.");
    }

    private static void LogFailure(string? path, Exception exception)
    {
        if (Interlocked.Increment(ref s_loggedFailures) > MaximumLoggedFailures)
            return;

        try
        {
            s_log.Error($"Could not redirect {path}: {exception.Message}");
        }
        catch (IOException)
        {
        }
    }
}
