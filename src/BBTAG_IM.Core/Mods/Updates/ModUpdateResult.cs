namespace BBTAG_IM.Core.Mods.Updates;

public enum ModUpdateOutcome
{
    UpToDate,
    Updated,
}

public sealed record ModUpdateResult(string ModName, ModUpdateOutcome Outcome, string Version, int Downloaded, int Deleted)
{
    public string Message => Outcome == ModUpdateOutcome.UpToDate
        ? $"{ModName} is up to date ({Version})."
        : $"{ModName} was updated to {Version}: {Downloaded} {(Downloaded == 1 ? "file" : "files")} downloaded, {Deleted} removed.";
}
