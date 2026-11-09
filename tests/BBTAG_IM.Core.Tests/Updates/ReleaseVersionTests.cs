using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Updates;

public class ReleaseVersionTests
{
    [Theory]
    [InlineData("0.2.0", "0.2.0")]
    [InlineData("v0.2.0", "0.2.0")]
    [InlineData("V1.10", "1.10")]
    [InlineData("v0.3.1-beta", "0.3.1")]
    [InlineData("BBTAG_IM 0.4.0", "0.4.0")]
    public void FromTag_TagWithAVersion_ReadsIt(string tag, string expected)
    {
        // Act
        Version? version = ReleaseVersion.FromTag(tag);

        // Assert
        Assert.Equal(Version.Parse(expected), version);
    }

    [Theory]
    [InlineData("Release")]
    [InlineData("")]
    [InlineData("v")]
    public void FromTag_TagWithoutAVersion_ReturnsNull(string tag)
    {
        // Act
        Version? version = ReleaseVersion.FromTag(tag);

        // Assert
        Assert.Null(version);
    }

    [Theory]
    [InlineData("0.2.0", "0.2.0.0", false)]
    [InlineData("0.2.1", "0.2.0.0", true)]
    [InlineData("0.10.0", "0.9.0.0", true)]
    [InlineData("0.1.9", "0.2.0.0", false)]
    public void IsNewer_ReleaseAgainstInstalled_ComparesMissingPartsAsZero(string release, string installed, bool expected)
    {
        // Act
        bool newer = ReleaseVersion.IsNewer(Version.Parse(release), Version.Parse(installed));

        // Assert
        Assert.Equal(expected, newer);
    }

    [Theory]
    [InlineData("0.2.0.0", "0.2.0")]
    [InlineData("1.0.0.3", "1.0.0.3")]
    [InlineData("1.2", "1.2.0")]
    public void Text_Version_DropsTheZeroRevision(string version, string expected)
    {
        // Act
        string text = ReleaseVersion.Text(Version.Parse(version));

        // Assert
        Assert.Equal(expected, text);
    }
}
