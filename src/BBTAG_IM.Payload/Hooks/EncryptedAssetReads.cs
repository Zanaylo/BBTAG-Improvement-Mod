using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods.Assets;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.Payload.Hooks;

internal static unsafe class EncryptedAssetReads
{
    private const string Kernel32Dll = "KERNEL32.dll";
    private const nint InvalidHandle = -1;
    private const uint FileCurrent = 1;
    private const uint OverlappedFlag = 0x40000000;
    private const int MaximumLoggedOpens = 20;
    private const int MaximumLoggedFailures = 5;

    private static readonly ConcurrentDictionary<nint, int> s_keyStarts = new();

    private static ILog s_log = null!;
    private static int s_loggedOpens;
    private static int s_loggedFailures;

    private static delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, FileOverlapped*, int> s_readFile;
    private static delegate* unmanaged[Stdcall]<nint, int> s_closeHandle;

    public static void Install(LoadedModule game, ILog log)
    {
        s_log = log;
        s_readFile = (delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, FileOverlapped*, int>)ImportHook.Replace(
            game, Kernel32Dll, "ReadFile", (nint)(delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, FileOverlapped*, int>)&ReadFile);
        s_closeHandle = (delegate* unmanaged[Stdcall]<nint, int>)ImportHook.Replace(
            game, Kernel32Dll, "CloseHandle", (nint)(delegate* unmanaged[Stdcall]<nint, int>)&CloseHandle);
    }

    public static nint Track(int? xorKeyStart, string path, uint flags, nint handle)
    {
        if (handle == InvalidHandle || xorKeyStart is not { } keyStart)
            return handle;

        if ((flags & OverlappedFlag) != 0)
            LogFailure($"{path} was opened for overlapped reads, so it is read without decrypting.");

        s_keyStarts[handle] = keyStart;
        LogOpen(path);
        return handle;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int ReadFile(nint file, byte* buffer, uint length, uint* read, FileOverlapped* overlapped)
    {
        if (!s_keyStarts.TryGetValue(file, out var keyStart))
            return s_readFile(file, buffer, length, read, overlapped);

        var offset = overlapped is null ? CurrentPosition(file) : OffsetOf(overlapped);
        var succeeded = s_readFile(file, buffer, length, read, overlapped);

        if (succeeded != 0 && offset >= 0)
            EncryptedAssets.Decrypt(new Span<byte>(buffer, (int)BytesRead(read, overlapped)), offset, keyStart);

        return succeeded;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int CloseHandle(nint handle)
    {
        s_keyStarts.TryRemove(handle, out _);
        return s_closeHandle(handle);
    }

    private static long CurrentPosition(nint file)
    {
        var error = Marshal.GetLastSystemError();

        if (Kernel32.SetFilePointerEx(file, 0, out var position, FileCurrent))
            return position;

        LogFailure($"Could not read the position of an encrypted file (Windows error {Marshal.GetLastSystemError()}).");
        Marshal.SetLastSystemError(error);
        return -1;
    }

    private static long OffsetOf(FileOverlapped* overlapped) => ((long)overlapped->OffsetHigh << 32) | overlapped->Offset;

    private static uint BytesRead(uint* read, FileOverlapped* overlapped) =>
        read is not null ? *read : (uint)overlapped->InternalHigh;

    private static void LogOpen(string path)
    {
        if (Interlocked.Increment(ref s_loggedOpens) <= MaximumLoggedOpens)
            s_log.Info($"Reading {path} through the asset key, so the game gets the bytes it expects.");
    }

    private static void LogFailure(string message)
    {
        if (Interlocked.Increment(ref s_loggedFailures) > MaximumLoggedFailures)
            return;

        try
        {
            s_log.Error(message);
        }
        catch (IOException)
        {
        }
    }
}
