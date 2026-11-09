using BBTAG_IM.Core.Improvement;

namespace BBTAG_IM.Core.Updates;

public sealed record UpdateOffer(Version Version, GitHubAsset Asset, Uri Page);

public static class ImprovementModRelease
{
    public const string UpdaterFileName = "BBTAG_IM_Updater.exe";
    public const string ZipExtension = ".zip";
    public const string DevZipSuffix = "-dev" + ZipExtension;

    public static readonly GitHubRepository Repository = new("Zanaylo", "BBTAG-Improvement-Mod");

    public static string? DefaultLinkOf(string modName) =>
        IsUpdatedByTheUpdater(modName) ? Repository.Url : null;

    public static bool IsUpdatedByTheUpdater(string modName) =>
        modName.Equals(ImprovementModContract.ModFolderName, StringComparison.OrdinalIgnoreCase);

    public static UpdateOffer? OfferFor(GitHubRelease release, InstalledCopy installed)
    {
        if (ReleaseVersion.FromTag(release.Tag) is not { } version || !ReleaseVersion.IsNewer(version, installed.Version))
            return null;

        return release.Assets.FirstOrDefault(asset => IsFor(asset, installed.Edition)) is { } zip ? new UpdateOffer(version, zip, release.Page) : null;
    }

    private static bool IsFor(GitHubAsset asset, Edition edition)
    {
        var isDev = asset.Name.EndsWith(DevZipSuffix, StringComparison.OrdinalIgnoreCase);
        var isZip = asset.Name.EndsWith(ZipExtension, StringComparison.OrdinalIgnoreCase);

        return isZip && isDev == (edition == Edition.Dev);
    }
}
