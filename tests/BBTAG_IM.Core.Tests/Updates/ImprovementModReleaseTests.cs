using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Updates;

public class ImprovementModReleaseTests
{
    private static readonly Uri Page = new("https://github.com/Zanaylo/BBTAG-Improvement-Mod/releases/tag/v0.3.0");
    private static readonly GitHubAsset PlayerZip = Asset(1, "BBTAG_IM-0.3.0.zip");
    private static readonly GitHubAsset DevZip = Asset(2, "BBTAG_IM-0.3.0-dev.zip");
    private static readonly InstalledCopy Player = new(Edition.Player, new Version(0, 2, 0, 0));
    private static readonly InstalledCopy Dev = new(Edition.Dev, new Version(0, 2, 0, 0));

    private static GitHubAsset Asset(long id, string name) =>
        new(id, name, 100, new Uri("https://github.com/Zanaylo/BBTAG-Improvement-Mod/releases/download/v0.3.0/" + name), DateTimeOffset.UnixEpoch);

    private static GitHubRelease Release(string tag, params GitHubAsset[] assets) => new(tag, Page, assets);

    [Fact]
    public void OfferFor_NewerReleaseAndPlayerEdition_OffersThePlayerZip()
    {
        // Act
        UpdateOffer? offer = ImprovementModRelease.OfferFor(Release("v0.3.0", PlayerZip, DevZip), Player);

        // Assert
        Assert.Equal(new UpdateOffer(new Version(0, 3, 0), PlayerZip, Page), offer);
    }

    [Fact]
    public void OfferFor_NewerReleaseAndDevEdition_OffersTheDevZip()
    {
        // Act
        UpdateOffer? offer = ImprovementModRelease.OfferFor(Release("v0.3.0", DevZip, PlayerZip), Dev);

        // Assert
        Assert.Equal(DevZip, offer?.Asset);
    }

    [Fact]
    public void OfferFor_SameVersion_ReturnsNull()
    {
        // Act
        UpdateOffer? offer = ImprovementModRelease.OfferFor(Release("v0.2.0", PlayerZip, DevZip), Player);

        // Assert
        Assert.Null(offer);
    }

    [Fact]
    public void OfferFor_TagWithoutAVersion_ReturnsNull()
    {
        // Act
        UpdateOffer? offer = ImprovementModRelease.OfferFor(Release("latest", PlayerZip, DevZip), Player);

        // Assert
        Assert.Null(offer);
    }

    [Fact]
    public void DefaultLinkOf_ImprovementModFolder_IsThisRepository()
    {
        // Act
        string? link = ImprovementModRelease.DefaultLinkOf("bbtag-improvement-mod");

        // Assert
        Assert.Equal("https://github.com/Zanaylo/BBTAG-Improvement-Mod", link);
    }

    [Fact]
    public void DefaultLinkOf_OtherMod_IsNull()
    {
        // Act
        string? link = ImprovementModRelease.DefaultLinkOf("BBTAG-OverHaul");

        // Assert
        Assert.Null(link);
    }

    [Fact]
    public void OfferFor_ReleaseWithoutTheEditionZip_ReturnsNull()
    {
        // Act
        UpdateOffer? offer = ImprovementModRelease.OfferFor(Release("v0.3.0", DevZip), Player);

        // Assert
        Assert.Null(offer);
    }
}
