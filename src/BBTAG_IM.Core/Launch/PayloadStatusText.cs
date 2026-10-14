namespace BBTAG_IM.Core.Launch;

public static class PayloadStatusText
{
    public static string Describe(PayloadStatus status) => status switch
    {
        PayloadStatus.Ready => "The mod is loaded.",
        PayloadStatus.InvalidSession => "The Mod Loader and BBTAG_IM.dll do not match. Use the files from the same release.",
        PayloadStatus.InvalidModFolder => "A mod folder could not be read.",
        PayloadStatus.IncompatibleExecutable => "The mod's BBTAG.exe was made for a different version of the game.",
        PayloadStatus.PatchFailed => "The mod's changes to the game code could not be applied.",
        PayloadStatus.HookFailed => "The mod could not take over the game's file loading.",
        PayloadStatus.Unexpected => "The mod failed while loading.",
        _ => $"The mod stopped with an unknown status ({(uint)status}).",
    };
}
