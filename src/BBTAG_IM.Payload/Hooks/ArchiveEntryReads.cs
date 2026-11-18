using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Mods.Assets;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.Payload.Hooks;

internal static unsafe class ArchiveEntryReads
{
    private const string Kernel32Dll = "KERNEL32.dll";
    private const nint InvalidHandle = -1;
    private const uint GenericRead = 0x80000000;
    private const uint ShareEverything = 0x00000007;
    private const uint OpenExisting = 3;
    private const uint NormalFile = 0x00000080;
    private const uint InvalidSetFilePointer = 0xFFFFFFFF;
    private const int Succeeded = 1;
    private const int Failed = 0;
    private const int ErrorFileNotFound = 2;
    private const int ErrorHandleEof = 38;
    private const int ErrorNegativeSeek = 131;
    private const int NoError = 0;
    private const int MaximumLoggedOpens = 20;
    private const int MaximumLoggedFailures = 5;

    private static readonly ConcurrentDictionary<nint, MemoryFile> s_files = new();

    private static ArchiveFileSystem s_archives = null!;
    private static ModFileRedirector s_redirector = null!;
    private static ILog s_log = null!;
    private static int s_loggedOpens;
    private static int s_loggedFailures;

    private static delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, FileOverlapped*, int> s_readFile;
    private static delegate* unmanaged[Stdcall]<nint, int, int*, uint, uint> s_setFilePointer;
    private static delegate* unmanaged[Stdcall]<nint, long, long*, uint, int> s_setFilePointerEx;
    private static delegate* unmanaged[Stdcall]<nint, uint*, uint> s_getFileSize;
    private static delegate* unmanaged[Stdcall]<nint, int> s_closeHandle;

    public static void Install(LoadedModule game, ArchiveFileSystem archives, ModFileRedirector redirector, ILog log)
    {
        s_archives = archives;
        s_redirector = redirector;
        s_log = log;
        s_readFile = (delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, FileOverlapped*, int>)ImportHook.Replace(
            game, Kernel32Dll, "ReadFile", (nint)(delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, FileOverlapped*, int>)&ReadFile);
        s_setFilePointer = (delegate* unmanaged[Stdcall]<nint, int, int*, uint, uint>)ImportHook.Replace(
            game, Kernel32Dll, "SetFilePointer", (nint)(delegate* unmanaged[Stdcall]<nint, int, int*, uint, uint>)&SetFilePointer);
        s_setFilePointerEx = (delegate* unmanaged[Stdcall]<nint, long, long*, uint, int>)ImportHook.Replace(
            game, Kernel32Dll, "SetFilePointerEx", (nint)(delegate* unmanaged[Stdcall]<nint, long, long*, uint, int>)&SetFilePointerEx);
        s_getFileSize = (delegate* unmanaged[Stdcall]<nint, uint*, uint>)ImportHook.Replace(
            game, Kernel32Dll, "GetFileSize", (nint)(delegate* unmanaged[Stdcall]<nint, uint*, uint>)&GetFileSize);
        s_closeHandle = (delegate* unmanaged[Stdcall]<nint, int>)ImportHook.Replace(
            game, Kernel32Dll, "CloseHandle", (nint)(delegate* unmanaged[Stdcall]<nint, int>)&CloseHandle);
    }

    public static bool Serves(string path) => s_archives.ArchiveOf(path) is not null;

    public static nint Open(string? requested, string path)
    {
        try
        {
            var bytes = s_archives.ReadAllBytes(path);

            if (s_redirector.XorKeyStartFor(requested, path) is { } keyStart)
                EncryptedAssets.Decrypt(bytes, 0, keyStart);

            var handle = Kernel32.CreateFile(s_archives.ArchiveOf(path)!, GenericRead, ShareEverything, 0, OpenExisting, NormalFile, 0);

            if (handle == InvalidHandle)
                return InvalidHandle;

            s_files[handle] = new MemoryFile(bytes);
            LogOpen(path);
            return handle;
        }
        catch (Exception exception)
        {
            LogFailure($"{path} could not be read from its archive: {exception.Message}");
            Marshal.SetLastSystemError(ErrorFileNotFound);
            return InvalidHandle;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int ReadFile(nint file, byte* buffer, uint length, uint* read, FileOverlapped* overlapped)
    {
        if (!s_files.TryGetValue(file, out var memory))
            return s_readFile(file, buffer, length, read, overlapped);

        var destination = new Span<byte>(buffer, (int)length);
        var count = overlapped is null ? memory.Read(destination) : memory.ReadAt(OffsetOf(overlapped), destination);

        if (read is not null)
            *read = (uint)count;

        if (overlapped is null)
            return Succeeded;

        overlapped->Internal = 0;
        overlapped->InternalHigh = (nuint)count;

        if (overlapped->Event != 0)
            Kernel32.SetEvent(overlapped->Event);

        if (count > 0 || length == 0)
            return Succeeded;

        Marshal.SetLastSystemError(ErrorHandleEof);
        return Failed;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint SetFilePointer(nint file, int distanceLow, int* distanceHigh, uint method)
    {
        if (!s_files.TryGetValue(file, out var memory))
            return s_setFilePointer(file, distanceLow, distanceHigh, method);

        var distance = distanceHigh is null ? distanceLow : ((long)*distanceHigh << 32) | (uint)distanceLow;

        if (memory.Seek(distance, (SeekOrigin)method) is not { } position)
        {
            Marshal.SetLastSystemError(ErrorNegativeSeek);
            return InvalidSetFilePointer;
        }

        if (distanceHigh is not null)
            *distanceHigh = (int)(position >> 32);

        Marshal.SetLastSystemError(NoError);
        return (uint)position;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int SetFilePointerEx(nint file, long distance, long* newPosition, uint method)
    {
        if (!s_files.TryGetValue(file, out var memory))
            return s_setFilePointerEx(file, distance, newPosition, method);

        if (memory.Seek(distance, (SeekOrigin)method) is not { } position)
        {
            Marshal.SetLastSystemError(ErrorNegativeSeek);
            return Failed;
        }

        if (newPosition is not null)
            *newPosition = position;

        return Succeeded;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint GetFileSize(nint file, uint* high)
    {
        if (!s_files.TryGetValue(file, out var memory))
            return s_getFileSize(file, high);

        if (high is not null)
            *high = (uint)(memory.Length >> 32);

        Marshal.SetLastSystemError(NoError);
        return (uint)memory.Length;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int CloseHandle(nint handle)
    {
        s_files.TryRemove(handle, out _);
        return s_closeHandle(handle);
    }

    private static long OffsetOf(FileOverlapped* overlapped) => ((long)overlapped->OffsetHigh << 32) | overlapped->Offset;

    private static void LogOpen(string path)
    {
        if (Interlocked.Increment(ref s_loggedOpens) <= MaximumLoggedOpens)
            s_log.Info($"Reading {path} straight from its archive.");
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
