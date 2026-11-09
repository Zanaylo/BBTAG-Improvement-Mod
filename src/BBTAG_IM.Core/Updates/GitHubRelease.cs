namespace BBTAG_IM.Core.Updates;

public sealed record GitHubAsset(long Id, string Name, long Size, Uri DownloadUrl, DateTimeOffset UpdatedAt);

public sealed record GitHubRelease(string Tag, Uri Page, IReadOnlyList<GitHubAsset> Assets);

public sealed record GitTreeFile(string Path, string Sha, long Size);
