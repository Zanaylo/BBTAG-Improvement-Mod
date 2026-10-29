using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

public static unsafe partial class Kernel32
{
    public const uint MemoryCommit = 0x1000;
    public const uint MemoryReserve = 0x2000;
    public const uint MemoryRelease = 0x8000;
    public const uint PageReadWrite = 0x04;
    public const uint PageExecuteReadWrite = 0x40;

    private const string Library = "kernel32.dll";
    private const int MaximumPathLength = 32768;

    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsDebuggerPresent();

    [LibraryImport(Library, SetLastError = true)]
    public static partial nint VirtualAlloc(nint address, nuint size, uint allocationType, uint protect);

    [LibraryImport(Library)]
    public static partial int MultiByteToWideChar(uint codePage, uint flags, byte* text, int length, char* wide, int wideLength);

    [LibraryImport(Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualFree(nint address, nuint size, uint freeType);

    [LibraryImport(Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualProtect(nint address, nuint size, uint newProtect, out uint oldProtect);

    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FlushInstructionCache(nint process, nint address, nuint size);

    [LibraryImport(Library)]
    public static partial nint GetCurrentProcess();

    [LibraryImport(Library)]
    public static partial uint GetCurrentProcessId();

    [LibraryImport(Library)]
    public static partial uint GetCurrentThreadId();

    [LibraryImport(Library)]
    public static partial nint AddVectoredExceptionHandler(uint first, nint handler);

    [LibraryImport(Library)]
    public static partial uint GetProcessId(nint process);

    [LibraryImport(Library, EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandle(string? moduleName);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint GetProcAddress(nint module, string procedureName);

    [LibraryImport(Library, EntryPoint = "LoadLibraryW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint LoadLibrary(string fileName);

    [LibraryImport(Library, EntryPoint = "CreateWaitableTimerExW", SetLastError = true)]
    public static partial nint CreateWaitableTimerEx(nint attributes, nint name, uint flags, uint desiredAccess);

    [LibraryImport(Library, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWaitableTimer(
        nint timer, in long dueTime, int period, nint completionRoutine, nint completionArgument, [MarshalAs(UnmanagedType.Bool)] bool resume);

    [LibraryImport(Library)]
    public static partial uint WaitForSingleObject(nint handle, uint milliseconds);

    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint process, nint address, byte* buffer, nuint length, out nuint read);

    [LibraryImport(Library)]
    public static partial nuint VirtualQuery(nint address, out MemoryRegion region, nuint length);

    [LibraryImport(Library, EntryPoint = "GetModuleFileNameW", SetLastError = true)]
    private static partial uint GetModuleFileName(nint module, char* fileName, uint size);

    public static string ModulePath(nint module)
    {
        var buffer = new char[MaximumPathLength];

        fixed (char* fileName = buffer)
        {
            var length = GetModuleFileName(module, fileName, MaximumPathLength);
            return new string(fileName, 0, (int)length);
        }
    }
}
