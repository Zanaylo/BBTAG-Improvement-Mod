namespace BBTAG_IM.Core.Updates;

public sealed class UpdaterSettings
{
    public const string FileName = "Updater.json";

    public string? SkippedVersion { get; set; }

    public void Skip(Version version) => SkippedVersion = ReleaseVersion.Text(version);

    public bool Skips(Version version) =>
        Version.TryParse(SkippedVersion, out var skipped) && !ReleaseVersion.IsNewer(version, skipped);
}
