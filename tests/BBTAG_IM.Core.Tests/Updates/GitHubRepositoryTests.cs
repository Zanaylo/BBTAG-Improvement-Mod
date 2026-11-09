using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Updates;

public class GitHubRepositoryTests
{
    [Theory]
    [InlineData("https://github.com/ZeroTwo-0/BBTAG-OverHaul-Patches")]
    [InlineData("https://github.com/ZeroTwo-0/BBTAG-OverHaul-Patches/")]
    [InlineData("https://github.com/ZeroTwo-0/BBTAG-OverHaul-Patches.git")]
    [InlineData("http://www.github.com/ZeroTwo-0/BBTAG-OverHaul-Patches")]
    [InlineData("github.com/ZeroTwo-0/BBTAG-OverHaul-Patches")]
    [InlineData("https://github.com/ZeroTwo-0/BBTAG-OverHaul-Patches/releases/tag/Release")]
    [InlineData("  ZeroTwo-0/BBTAG-OverHaul-Patches  ")]
    public void TryParse_GitHubLink_ReadsOwnerAndName(string text)
    {
        // Act
        GitHubRepository? repository = GitHubRepository.TryParse(text);

        // Assert
        Assert.Equal(new GitHubRepository("ZeroTwo-0", "BBTAG-OverHaul-Patches"), repository);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://gitlab.com/someone/mod")]
    [InlineData("gitlab.com/someone/mod")]
    [InlineData("https://github.com/someone")]
    [InlineData("just words")]
    public void TryParse_NotAGitHubRepository_ReturnsNull(string? text)
    {
        // Act
        GitHubRepository? repository = GitHubRepository.TryParse(text);

        // Assert
        Assert.Null(repository);
    }

    [Fact]
    public void Url_Repository_IsTheGitHubPage()
    {
        // Arrange
        GitHubRepository repository = new("ZeroTwo-0", "BBTAG-OverHaul-Patches");

        // Act
        string url = repository.Url;

        // Assert
        Assert.Equal("https://github.com/ZeroTwo-0/BBTAG-OverHaul-Patches", url);
    }

    [Fact]
    public void RawFile_PathWithSpaces_EscapesEachPart()
    {
        // Arrange
        GitHubRepository repository = new("owner", "mod");

        // Act
        Uri address = repository.RawFile("abc123", "data/my folder/file #1.pac");

        // Assert
        Assert.Equal("https://raw.githubusercontent.com/owner/mod/abc123/data/my%20folder/file%20%231.pac", address.AbsoluteUri);
    }
}
