using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using BBTAG_IM.Launcher.Native;

namespace BBTAG_IM.Launcher.Injection;

internal sealed unsafe class RemoteProcess : IDisposable
{
    private const uint Infinite = uint.MaxValue;

    private readonly nint _process;
    private readonly nint _mainThread;
    private bool _disposed;

    private RemoteProcess(ProcessInformation information)
    {
        _process = information.Process;
        _mainThread = information.Thread;
        ProcessId = information.ProcessId;
    }

    public uint ProcessId { get; }

    public static RemoteProcess StartSuspended(string executable, string workingDirectory)
    {
        var commandLine = $"\"{executable}\"\0".ToCharArray();
        var startup = new StartupInfo { Size = sizeof(StartupInfo) };
        ProcessInformation information;

        fixed (char* command = commandLine)
        {
            if (!NativeMethods.CreateProcess(executable, command, 0, 0, false, NativeMethods.CreateSuspended, 0, workingDirectory, &startup, &information))
                throw new LaunchFailedException($"Windows could not start {executable} (error {Marshal.GetLastPInvokeError()}).");
        }

        return new RemoteProcess(information);
    }

    public uint ImageBase()
    {
        ProcessBasicInformation information;
        var status = NativeMethods.NtQueryInformationProcess(_process, NativeMethods.ProcessBasicInformationClass, &information, (uint)sizeof(ProcessBasicInformation), out _);

        if (status != 0)
            throw new LaunchFailedException($"Could not read the game's process information (status 0x{status:X8}).");

        return BinaryPrimitives.ReadUInt32LittleEndian(Read(information.PebBaseAddress + NativeMethods.PebImageBaseOffset, sizeof(uint)));
    }

    public byte[] Read(nint address, int length)
    {
        var buffer = new byte[length];

        fixed (byte* destination = buffer)
        {
            if (!NativeMethods.ReadProcessMemory(_process, address, destination, (nuint)length, out var read) || read != (nuint)length)
                throw new LaunchFailedException($"Could not read the game's memory at 0x{address:X8} (error {Marshal.GetLastPInvokeError()}).");
        }

        return buffer;
    }

    public void Write(nint address, ReadOnlySpan<byte> bytes)
    {
        NativeMethods.VirtualProtectEx(_process, address, (nuint)bytes.Length, NativeMethods.PageExecuteReadWrite, out var previous);

        fixed (byte* source = bytes)
        {
            if (!NativeMethods.WriteProcessMemory(_process, address, source, (nuint)bytes.Length, out var written) || written != (nuint)bytes.Length)
                throw new LaunchFailedException($"Could not write the game's memory at 0x{address:X8} (error {Marshal.GetLastPInvokeError()}).");
        }

        NativeMethods.VirtualProtectEx(_process, address, (nuint)bytes.Length, previous, out _);
        NativeMethods.FlushInstructionCache(_process, address, (nuint)bytes.Length);
    }

    public RemoteBuffer WriteString(string text)
    {
        var bytes = Encoding.Unicode.GetBytes(text + "\0");
        var address = NativeMethods.VirtualAllocEx(_process, 0, (nuint)bytes.Length, NativeMethods.MemoryReserve | NativeMethods.MemoryCommit, NativeMethods.PageReadWrite);

        if (address == 0)
            throw new LaunchFailedException($"Could not allocate memory in the game (error {Marshal.GetLastPInvokeError()}).");

        Write(address, bytes);
        return new RemoteBuffer(this, address);
    }

    public uint RunThread(nint startAddress, nint parameter, TimeSpan timeout)
    {
        var thread = NativeMethods.CreateRemoteThread(_process, 0, 0, startAddress, parameter, 0, out _);

        if (thread == 0)
            throw new LaunchFailedException($"Could not start a thread in the game (error {Marshal.GetLastPInvokeError()}).");

        try
        {
            if (NativeMethods.WaitForSingleObject(thread, (uint)timeout.TotalMilliseconds) != NativeMethods.WaitObject0)
                throw new LaunchFailedException($"The game did not finish loading the mod within {timeout.TotalSeconds:0} seconds.");

            NativeMethods.GetExitCodeThread(thread, out var exitCode);
            return exitCode;
        }
        finally
        {
            NativeMethods.CloseHandle(thread);
        }
    }

    public uint MainThreadInstructionPointer()
    {
        var context = stackalloc byte[NativeMethods.ContextSize];
        new Span<byte>(context, NativeMethods.ContextSize).Clear();
        *(uint*)context = NativeMethods.ContextControl;

        NativeMethods.SuspendThread(_mainThread);

        try
        {
            if (!NativeMethods.GetThreadContext(_mainThread, context))
                throw new LaunchFailedException($"Could not read the game's main thread (error {Marshal.GetLastPInvokeError()}).");

            return *(uint*)(context + NativeMethods.ContextInstructionPointerOffset);
        }
        finally
        {
            NativeMethods.ResumeThread(_mainThread);
        }
    }

    public void SuspendMainThread() => NativeMethods.SuspendThread(_mainThread);

    public void ResumeMainThread() => NativeMethods.ResumeThread(_mainThread);

    public void Free(nint address) => NativeMethods.VirtualFreeEx(_process, address, 0, NativeMethods.MemoryRelease);

    public void Terminate() => NativeMethods.TerminateProcess(_process, Infinite);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        NativeMethods.CloseHandle(_mainThread);
        NativeMethods.CloseHandle(_process);
    }
}

internal sealed class RemoteBuffer(RemoteProcess process, nint address) : IDisposable
{
    public nint Address => address;

    public void Dispose() => process.Free(address);
}
