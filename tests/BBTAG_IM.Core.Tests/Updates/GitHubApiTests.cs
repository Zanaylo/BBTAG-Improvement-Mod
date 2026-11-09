using System.Net;
using BBTAG_IM.Core.Tests.Support;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Updates;

public class GitHubApiTests
{
    private const string ReleaseAddress = "https://api.github.com/repos/owner/mod/releases/latest";
    private const string HeadAddress = "https://api.github.com/repos/owner/mod/commits/HEAD";
    private const string TreeAddress = "https://api.github.com/repos/owner/mod/git/trees/abc123?recursive=1";

    private const string ReleaseJson = """
        {
          "tag_name": "v0.3.0",
          "html_url": "https://github.com/owner/mod/releases/tag/v0.3.0",
          "assets": [
            {
              "id": 77,
              "name": "Mod-0.3.0.zip",
              "size": 1234,
              "browser_download_url": "https://github.com/owner/mod/releases/download/v0.3.0/Mod-0.3.0.zip",
              "updated_at": "2026-11-09T08:20:54Z"
            }
          ]
        }
        """;

    private const string TreeJson = """
        {
          "sha": "abc123",
          "truncated": false,
          "tree": [
            { "path": "data", "mode": "040000", "type": "tree", "sha": "d1" },
            { "path": "data/char.pac", "mode": "100644", "type": "blob", "sha": "f1", "size": 10 },
            { "path": "BBTAG.exe", "mode": "100755", "type": "blob", "sha": "f2", "size": 20 },
            { "path": "link", "mode": "120000", "type": "blob", "sha": "f3", "size": 4 },
            { "path": "sub", "mode": "160000", "type": "commit", "sha": "f4" }
          ]
        }
        """;

    private static readonly GitHubRepository Repository = new("owner", "mod");

    private readonly FakeHttpHandler _handler = new();

    public GitHubApi CreateGitHubApi() => new(new HttpClient(_handler));

    [Fact]
    public async Task LatestReleaseAsync_ReleaseWithAnAsset_ReadsTagPageAndAsset()
    {
        // Arrange
        _handler.Answer(ReleaseAddress, ReleaseJson);

        // Act
        GitHubRelease? release = await CreateGitHubApi().LatestReleaseAsync(Repository, CancellationToken.None);

        // Assert
        Assert.NotNull(release);
        Assert.Equal("v0.3.0", release.Tag);
        Assert.Equal(new Uri("https://github.com/owner/mod/releases/tag/v0.3.0"), release.Page);
        GitHubAsset asset = Assert.Single(release.Assets);
        Assert.Equal(new GitHubAsset(77, "Mod-0.3.0.zip", 1234, new Uri("https://github.com/owner/mod/releases/download/v0.3.0/Mod-0.3.0.zip"), DateTimeOffset.Parse("2026-11-09T08:20:54Z")), asset);
    }

    [Fact]
    public async Task LatestReleaseAsync_NoRelease_ReturnsNull()
    {
        // Act
        GitHubRelease? release = await CreateGitHubApi().LatestReleaseAsync(Repository, CancellationToken.None);

        // Assert
        Assert.Null(release);
    }

    [Fact]
    public async Task LatestReleaseAsync_Request_IdentifiesTheModToGitHub()
    {
        // Arrange
        _handler.Answer(ReleaseAddress, ReleaseJson);

        // Act
        await CreateGitHubApi().LatestReleaseAsync(Repository, CancellationToken.None);

        // Assert
        HttpRequestMessage request = Assert.Single(_handler.Requests);
        Assert.Equal("BBTAG-Improvement-Mod", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task HeadCommitAsync_Repository_ReturnsTheShaGitHubSends()
    {
        // Arrange
        _handler.Answer(HeadAddress, "f965cea7ae5b5529533c73d7cdf8e4b02e29818c");

        // Act
        string commit = await CreateGitHubApi().HeadCommitAsync(Repository, CancellationToken.None);

        // Assert
        Assert.Equal("f965cea7ae5b5529533c73d7cdf8e4b02e29818c", commit);
        Assert.Equal("application/vnd.github.sha", Assert.Single(_handler.Requests).Headers.Accept.ToString());
    }

    [Fact]
    public async Task HeadCommitAsync_UnknownRepository_ThrowsWithTheLink()
    {
        // Act
        Exception? exception = await Record.ExceptionAsync(() => CreateGitHubApi().HeadCommitAsync(Repository, CancellationToken.None));

        // Assert
        GitHubException error = Assert.IsType<GitHubException>(exception);
        Assert.Contains("https://github.com/owner/mod", error.Message);
    }

    [Fact]
    public async Task HeadCommitAsync_RateLimited_SaysToTryLater()
    {
        // Arrange
        _handler.Answer(HeadAddress, () =>
        {
            HttpResponseMessage response = new(HttpStatusCode.Forbidden);
            response.Headers.Add("X-RateLimit-Remaining", "0");
            return response;
        });

        // Act
        Exception? exception = await Record.ExceptionAsync(() => CreateGitHubApi().HeadCommitAsync(Repository, CancellationToken.None));

        // Assert
        GitHubException error = Assert.IsType<GitHubException>(exception);
        Assert.Contains("later", error.Message);
    }

    [Fact]
    public async Task TreeAsync_Tree_ReturnsOnlyRegularFiles()
    {
        // Arrange
        _handler.Answer(TreeAddress, TreeJson);

        // Act
        IReadOnlyList<GitTreeFile> files = await CreateGitHubApi().TreeAsync(Repository, "abc123", CancellationToken.None);

        // Assert
        Assert.Equal([new GitTreeFile("data/char.pac", "f1", 10), new GitTreeFile("BBTAG.exe", "f2", 20)], files);
    }

    [Fact]
    public async Task TreeAsync_TruncatedTree_Throws()
    {
        // Arrange
        _handler.Answer(TreeAddress, """{ "truncated": true, "tree": [] }""");

        // Act
        Exception? exception = await Record.ExceptionAsync(() => CreateGitHubApi().TreeAsync(Repository, "abc123", CancellationToken.None));

        // Assert
        Assert.IsType<GitHubException>(exception);
    }

    [Fact]
    public async Task DownloadAsync_File_CopiesItAndReportsTheBytes()
    {
        // Arrange
        byte[] body = [1, 2, 3, 4, 5];
        _handler.Answer("https://example.com/file.bin", body);
        using MemoryStream target = new();
        List<long> reported = [];

        // Act
        await CreateGitHubApi().DownloadAsync(new Uri("https://example.com/file.bin"), target, new SynchronousProgress<long>(reported.Add), CancellationToken.None);

        // Assert
        Assert.Equal(body, target.ToArray());
        Assert.Equal(5, reported[^1]);
    }

    [Fact]
    public async Task DownloadAsync_MissingFile_Throws()
    {
        // Arrange
        using MemoryStream target = new();

        // Act
        Exception? exception = await Record.ExceptionAsync(() => CreateGitHubApi().DownloadAsync(new Uri("https://example.com/missing.bin"), target, new SynchronousProgress<long>(_ => { }), CancellationToken.None));

        // Assert
        Assert.IsType<GitHubException>(exception);
    }
}
