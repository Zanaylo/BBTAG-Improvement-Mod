namespace BBTAG_IM.Core.Updates;

public interface IGitHubApi
{
    Task<GitHubRelease?> LatestReleaseAsync(GitHubRepository repository, CancellationToken cancellation);

    Task<string> HeadCommitAsync(GitHubRepository repository, CancellationToken cancellation);

    Task<IReadOnlyList<GitTreeFile>> TreeAsync(GitHubRepository repository, string commit, CancellationToken cancellation);

    Task DownloadAsync(Uri address, Stream target, IProgress<long> progress, CancellationToken cancellation);
}
