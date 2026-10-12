namespace BBTAG_IM.Core.CheatEngine;

public static class RestartPolicy
{
    public const int MaximumAttempts = 3;

    public static readonly TimeSpan StartupWindow = TimeSpan.FromMinutes(2);

    public static bool ShouldRestart(int attempt, TimeSpan attachedFor) =>
        attempt < MaximumAttempts && attachedFor < StartupWindow;
}
