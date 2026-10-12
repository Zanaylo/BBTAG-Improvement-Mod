using System.Diagnostics;

namespace BBTAG_IM.Launcher.Injection;

internal sealed class EntryPointGate
{
    private static readonly byte[] SpinInPlace = [0xEB, 0xFE];
    private static readonly TimeSpan ArrivalTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);

    private readonly RemoteProcess _process;
    private readonly uint _entryPoint;
    private readonly byte[] _originalBytes;

    private EntryPointGate(RemoteProcess process, uint entryPoint, byte[] originalBytes)
    {
        _process = process;
        _entryPoint = entryPoint;
        _originalBytes = originalBytes;
    }

    public static EntryPointGate HoldAtEntryPoint(RemoteProcess process, uint entryPoint)
    {
        var gate = new EntryPointGate(process, entryPoint, process.Read((nint)entryPoint, SpinInPlace.Length));
        process.Write((nint)entryPoint, SpinInPlace);
        process.ResumeMainThread();
        gate.WaitForArrival();
        return gate;
    }

    public void Release()
    {
        _process.SuspendMainThread();
        _process.Write((nint)_entryPoint, _originalBytes);
        _process.ResumeMainThread();
    }

    private void WaitForArrival()
    {
        var clock = Stopwatch.StartNew();

        while (_process.MainThreadInstructionPointer() != _entryPoint)
        {
            if (clock.Elapsed > ArrivalTimeout)
                throw new LaunchFailedException("The game did not finish starting up, so the mod was not loaded.");

            Thread.Sleep(PollInterval);
        }
    }
}
