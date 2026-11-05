using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.Payload.Hooks;

internal static unsafe class ExitHooks
{
    private const string Kernel32Dll = "KERNEL32.dll";
    private const int StackWordsScanned = 512;
    private const int AddressesLogged = 24;
    private const int ContinueSearch = 0;
    private const int ExceptionCodeField = 0;
    private const int ExceptionAddressField = 12;

    private static ILog s_log = null!;
    private static uint s_moduleStart;
    private static uint s_moduleEnd;
    private static nint s_gameFilter;

    private static delegate* unmanaged[Stdcall]<uint, void> s_exitProcess;
    private static delegate* unmanaged[Stdcall]<nint, uint, int> s_terminateProcess;
    private static delegate* unmanaged[Stdcall]<nint, nint> s_setUnhandledExceptionFilter;

    public static void Install(LoadedModule game, ILog log)
    {
        s_log = log;
        s_moduleStart = (uint)game.BaseAddress;
        s_moduleEnd = s_moduleStart + game.Headers.SizeOfImage;

        s_exitProcess = (delegate* unmanaged[Stdcall]<uint, void>)ImportHook.Replace(
            game, Kernel32Dll, "ExitProcess", (nint)(delegate* unmanaged[Stdcall]<uint, void>)&ExitProcess);
        s_terminateProcess = (delegate* unmanaged[Stdcall]<nint, uint, int>)ImportHook.Replace(
            game, Kernel32Dll, "TerminateProcess", (nint)(delegate* unmanaged[Stdcall]<nint, uint, int>)&TerminateProcess);
        s_setUnhandledExceptionFilter = (delegate* unmanaged[Stdcall]<nint, nint>)ImportHook.Replace(
            game, Kernel32Dll, "SetUnhandledExceptionFilter", (nint)(delegate* unmanaged[Stdcall]<nint, nint>)&SetUnhandledExceptionFilter);

        log.Info("Exit hooks installed: ExitProcess, TerminateProcess, SetUnhandledExceptionFilter.");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void ExitProcess(uint exitCode)
    {
        LogExit($"The game called ExitProcess({exitCode}).");
        s_exitProcess(exitCode);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int TerminateProcess(nint process, uint exitCode)
    {
        if (Kernel32.GetProcessId(process) == Kernel32.GetCurrentProcessId())
            LogExit($"The game called TerminateProcess on itself with exit code {exitCode}.");

        return s_terminateProcess(process, exitCode);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint SetUnhandledExceptionFilter(nint filter)
    {
        var previousGameFilter = s_gameFilter;
        s_gameFilter = filter;
        s_setUnhandledExceptionFilter((nint)(delegate* unmanaged[Stdcall]<nint, int>)&UnhandledException);
        return previousGameFilter;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int UnhandledException(nint exceptionPointers)
    {
        var record = *(nint*)exceptionPointers;
        var code = *(uint*)(record + ExceptionCodeField);
        var address = *(uint*)(record + ExceptionAddressField);
        LogExit(GameCrashText.Describe(code, address, ThrownMessage(record)));

        return s_gameFilter == 0 ? ContinueSearch : ((delegate* unmanaged[Stdcall]<nint, int>)s_gameFilter)(exceptionPointers);
    }

    private static string? ThrownMessage(nint record) => CppExceptionReader.Message(new ProcessMemory(), (uint)record);

    private static void LogExit(string message)
    {
        try
        {
            uint marker = 0;
            var stack = new ReadOnlySpan<uint>(&marker, ReadableStackWords((nint)(&marker)));
            var addresses = ExitTrace.GameAddresses(stack, s_moduleStart, s_moduleEnd, AddressesLogged);
            s_log.Error($"{message} Game addresses on the stack: {ExitTrace.Describe(addresses)}.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static int ReadableStackWords(nint start)
    {
        if (Kernel32.VirtualQuery(start, out var region, (nuint)sizeof(MemoryRegion)) == 0)
            return 0;

        var readable = (long)(region.End - (ulong)start) / sizeof(uint);
        return (int)Math.Min(readable, StackWordsScanned);
    }
}
