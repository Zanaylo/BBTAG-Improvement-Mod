namespace BBTAG_IM.Core.Launch;

public sealed class DebuggerWait(Func<bool> isAttached, TimeProvider time, Action<TimeSpan> sleep)
{
    public static readonly TimeSpan Limit = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    public bool Wait()
    {
        var deadline = time.GetUtcNow() + Limit;

        while (!isAttached())
        {
            if (time.GetUtcNow() >= deadline)
                return false;

            sleep(PollInterval);
        }

        return true;
    }
}
