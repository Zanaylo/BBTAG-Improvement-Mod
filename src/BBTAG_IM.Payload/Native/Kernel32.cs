using System.Runtime.InteropServices;

namespace BBTAG_IM.Payload.Native;

internal static unsafe partial class Kernel32
{
    public const uint MemoryCommit = 0x1000;
    public const uint MemoryReserve = 0x2000;
    public const uint MemoryRelease = 0x8000;
    public const uint PageReadWrite = 0x04;
    public const uint PageExecuteReadWrite = 0x40;

    private const string Library = "kernel32.dll";
    private const int MaximumPathLength = 32768;

    [LibraryImport(Library, SetLastError = true)]
    public static partial nint VirtualAlloc(nint address, nuint size, uint allocationType, uint protect);

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

    [LibraryImport(Library, EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandle(string? moduleName);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint GetProcAddress(nint module, string procedureName);

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
