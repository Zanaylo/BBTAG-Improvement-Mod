namespace BBTAG_IM.Core.Launch;

public enum PayloadStatus : uint
{
    Ready = 0,
    InvalidSession = 1,
    InvalidModFolder = 2,
    IncompatibleExecutable = 3,
    PatchFailed = 4,
    HookFailed = 5,
    Unexpected = 99,
}
