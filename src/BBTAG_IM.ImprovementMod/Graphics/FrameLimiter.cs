using System.Diagnostics;
using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Graphics;

internal sealed class FrameLimiter
{
    public const int FramesPerSecond = 60;

    private const uint HighResolutionTimer = 0x00000002;
    private const uint TimerAllAccess = 0x001F0003;
    private const uint Infinite = 0xFFFFFFFF;
    private const long HundredNanosecondsPerSecond = 10_000_000;
    private const long MillisecondsPerSecond = 1000;

    private static readonly long SpinTicks = Stopwatch.Frequency / MillisecondsPerSecond;

    private readonly FramePacer _pacer = new(Stopwatch.Frequency, FramesPerSecond);
    private readonly nint _timer = Kernel32.CreateWaitableTimerEx(0, 0, HighResolutionTimer, TimerAllAccess);

    public bool HasHighResolutionTimer => _timer != 0;

    public void WaitForNextFrame()
    {
        var now = Stopwatch.GetTimestamp();
        var deadline = now + _pacer.TicksToWait(now);

        Sleep(deadline - SpinTicks - now);

        while (Stopwatch.GetTimestamp() < deadline)
            Thread.SpinWait(1);
    }

    private void Sleep(long ticks)
    {
        if (ticks <= 0 || _timer == 0)
            return;

        var dueTime = -ticks * HundredNanosecondsPerSecond / Stopwatch.Frequency;

        if (Kernel32.SetWaitableTimer(_timer, in dueTime, 0, 0, 0, false))
            Kernel32.WaitForSingleObject(_timer, Infinite);
    }
}
