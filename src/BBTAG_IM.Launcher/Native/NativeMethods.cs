using System.Runtime.InteropServices;

namespace BBTAG_IM.Launcher.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct StartupInfo
{
    public int Size;
    public nint Reserved;
    public nint Desktop;
    public nint Title;
    public int X;
    public int Y;
    public int XSize;
    public int YSize;
    public int XCountChars;
    public int YCountChars;
    public int FillAttribute;
    public int Flags;
    public short ShowWindow;
    public short Reserved2Size;
    public nint Reserved2;
    public nint StandardInput;
    public nint StandardOutput;
    public nint StandardError;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ProcessInformation
{
    public nint Process;
    public nint Thread;
    public uint ProcessId;
    public uint ThreadId;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ProcessBasicInformation
{
    public nint ExitStatus;
    public nint PebBaseAddress;
    public nint AffinityMask;
    public nint BasePriority;
    public nint UniqueProcessId;
    public nint InheritedFromUniqueProcessId;
}

internal static unsafe partial class NativeMethods
{
    public const uint CreateSuspended = 0x00000004;
    public const uint MemoryCommit = 0x1000;
    public const uint MemoryReserve = 0x2000;
    public const uint MemoryRelease = 0x8000;
    public const uint PageReadWrite = 0x04;
    public const uint PageExecuteReadWrite = 0x40;
    public const uint WaitObject0 = 0x00000000;
    public const uint ContextControl = 0x00010001;
    public const int ContextSize = 0x2CC;
    public const int ContextInstructionPointerOffset = 0xB8;
    public const int ProcessBasicInformationClass = 0;
    public const int PebImageBaseOffset = 0x08;

    private const string Kernel32 = "kernel32.dll";
    private const string Ntdll = "ntdll.dll";

    [LibraryImport(Kernel32, EntryPoint = "CreateProcessW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreateProcess(
        string? applicationName,
        char* commandLine,
        nint processAttributes,
        nint threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        nint environment,
        string? currentDirectory,
        StartupInfo* startupInfo,
        ProcessInformation* processInformation);

    [LibraryImport(Kernel32, SetLastError = true)]
    public static partial nint VirtualAllocEx(nint process, nint address, nuint size, uint allocationType, uint protect);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualFreeEx(nint process, nint address, nuint size, uint freeType);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualProtectEx(nint process, nint address, nuint size, uint newProtect, out uint oldProtect);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(nint process, nint address, byte* buffer, nuint size, out nuint written);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint process, nint address, byte* buffer, nuint size, out nuint read);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FlushInstructionCache(nint process, nint address, nuint size);

    [LibraryImport(Kernel32, SetLastError = true)]
    public static partial nint CreateRemoteThread(nint process, nint threadAttributes, nuint stackSize, nint startAddress, nint parameter, uint creationFlags, out uint threadId);

    [LibraryImport(Kernel32, SetLastError = true)]
    public static partial uint WaitForSingleObject(nint handle, uint milliseconds);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetExitCodeThread(nint thread, out uint exitCode);

    [LibraryImport(Kernel32, SetLastError = true)]
    public static partial uint ResumeThread(nint thread);

    [LibraryImport(Kernel32, SetLastError = true)]
    public static partial uint SuspendThread(nint thread);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetThreadContext(nint thread, byte* context);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TerminateProcess(nint process, uint exitCode);

    [LibraryImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport(Kernel32, EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandle(string moduleName);

    [LibraryImport(Kernel32, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint GetProcAddress(nint module, string procedureName);

    [LibraryImport(Ntdll)]
    public static partial int NtQueryInformationProcess(nint process, int informationClass, ProcessBasicInformation* information, uint length, out uint returnLength);
}
